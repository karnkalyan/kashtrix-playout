// Kashtrix CG Controller Web API Client - Communicates with Kashtrix.ApiGateway
const CANDIDATE_BASES = [
  'http://127.0.0.1:9080',
  '', // Vite dev proxy / same-origin fallback
  'http://localhost:9080'
];

export const DEFAULT_API_KEY = 'kashtrix-local-test';

export function getApiKey() {
  return localStorage.getItem('kashtrix_api_key') || DEFAULT_API_KEY;
}

export function setApiKey(key) {
  localStorage.setItem('kashtrix_api_key', key);
}

export function getApiBase() {
  return localStorage.getItem('kashtrix_api_url') || 'http://127.0.0.1:9080';
}

export function setApiBase(url) {
  localStorage.setItem('kashtrix_api_url', url);
}

let activeWorkingBase = getApiBase();

async function executeFetch(base, endpoint, options) {
  const apiKey = getApiKey();
  const sep = endpoint.includes('?') ? '&' : '?';
  const url = `${base}${endpoint}${sep}apiKey=${encodeURIComponent(apiKey)}`;

  const headers = {
    'Accept': 'application/json',
    'X-Kashtrix-Api-Key': apiKey,
    ...(options.headers || {})
  };

  if (options.body && typeof options.body === 'object') {
    headers['Content-Type'] = 'application/json';
    options.body = JSON.stringify(options.body);
  }

  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), 4000);

  try {
    const res = await fetch(url, { ...options, headers, signal: controller.signal });
    clearTimeout(timeoutId);
    if (!res.ok) {
      const errText = await res.text();
      let parsed;
      try { parsed = JSON.parse(errText); } catch { parsed = { message: errText }; }
      return { ok: false, status: res.status, ...parsed };
    }
    return await res.json();
  } catch (err) {
    clearTimeout(timeoutId);
    return { ok: false, error: err.message, offline: true };
  }
}

async function request(endpoint, options = {}) {
  // 1. Try with currently stored / active base
  let res = await executeFetch(activeWorkingBase, endpoint, options);
  if (res && (res.ok || (res.status && res.status !== 0 && res.status !== 502 && res.status !== 503))) {
    return res;
  }

  // 2. If network error / offline, test fallback candidate bases automatically
  for (const candidate of CANDIDATE_BASES) {
    if (candidate === activeWorkingBase) continue;
    const testRes = await executeFetch(candidate, endpoint, options);
    if (testRes && (testRes.ok || (testRes.status && testRes.status !== 0))) {
      activeWorkingBase = candidate;
      setApiBase(candidate);
      return testRes;
    }
  }

  return res;
}

export const CgApi = {
  // Check backend health
  async checkHealth() {
    return await request('/api/v1/health');
  },

  // Read available CG targets (channels & playout engines)
  async getTargets() {
    return await request('/api/v1/cg-controller/targets');
  },

  // Read actual live CG templates from Kashtrix CG Database / Factory
  async getTemplates() {
    return await request('/api/v1/cg/templates');
  },

  // Send CG Play command (trigger template on Program or Preview bus)
  async play(targets, template, layer = 1, bus = 'PROGRAM', data = {}) {
    const targetArray = Array.isArray(targets) ? targets : [targets];
    return await request('/api/v1/cg-controller/play', {
      method: 'POST',
      body: {
        targets: targetArray,
        template,
        layer,
        bus,
        data
      }
    });
  },

  // Send CG Update command (live hot-update data without retriggering animation)
  async update(targets, template, layer = 1, bus = 'PROGRAM', data = {}) {
    const targetArray = Array.isArray(targets) ? targets : [targets];
    return await request('/api/v1/cg-controller/update', {
      method: 'POST',
      body: {
        targets: targetArray,
        template,
        layer,
        bus,
        data
      }
    });
  },

  // Send CG Play Preview command (trigger template on Preview bus)
  async playPreview(targets, template, layer = 1, data = {}) {
    return await this.play(targets, template, layer, 'PREVIEW', data);
  },

  // Take Preview to Program
  async take(targets, layer = 1, transition = 'CUT', template = null) {
    const targetArray = Array.isArray(targets) ? targets : [targets];
    return await request('/api/v1/cg-controller/take', {
      method: 'POST',
      body: {
        targets: targetArray,
        layer,
        bus: 'PROGRAM',
        transition,
        template
      }
    });
  },

  // Read live on-air CG items currently playing on PROGRAM
  async getOnAir(channelId = null) {
    const query = channelId ? `?channelId=${encodeURIComponent(channelId)}` : '';
    return await request(`/api/v1/cg-controller/onair${query}`);
  },

  // Stop CG Graphic (animates out). Can target specific project/layer or bus
  async stop(targets, layerOrOpts = 1, bus = 'PROGRAM') {
    const targetArray = Array.isArray(targets) ? targets : [targets];
    let body = { targets: targetArray, layer: 1, bus };
    if (typeof layerOrOpts === 'object' && layerOrOpts !== null) {
      body = {
        targets: targetArray,
        layer: layerOrOpts.layer !== undefined ? layerOrOpts.layer : 1,
        bus: layerOrOpts.bus || bus,
        projectId: layerOrOpts.projectId,
        template: layerOrOpts.template || layerOrOpts.name
      };
    } else {
      body.layer = layerOrOpts;
    }
    return await request('/api/v1/cg-controller/stop', {
      method: 'POST',
      body
    });
  },

  // Clear CG Graphic immediately (instant cut, no out animation)
  async clear(targets, layerOrOpts = -1, bus = 'PROGRAM') {
    const targetArray = Array.isArray(targets) ? targets : [targets];
    let body = { targets: targetArray, layer: -1, bus };
    if (typeof layerOrOpts === 'object' && layerOrOpts !== null) {
      body = {
        targets: targetArray,
        layer: layerOrOpts.layer !== undefined ? layerOrOpts.layer : -1,
        bus: layerOrOpts.bus || bus,
        projectId: layerOrOpts.projectId,
        template: layerOrOpts.template || layerOrOpts.name
      };
    } else {
      body.layer = layerOrOpts;
    }
    return await request('/api/v1/cg-controller/clear', {
      method: 'POST',
      body
    });
  },

  // Seed demo templates into database if empty
  async seedDemo() {
    return await request('/api/v1/demo/load', {
      method: 'POST'
    });
  },

  // Import .kcg / .kashtrixcg binary or JSON template via API
  async importCg(payload) {
    return await request('/api/v1/cg/import', {
      method: 'POST',
      body: payload
    });
  },

  // Playout channels & status sync (same as playout-web for base video / confidence monitor)
  async getPlayoutChannels() {
    return await request('/api/v1/playout/channels');
  },

  // Playout playlist
  async getPlayoutPlaylist(channelId = 'KTX-PLAYOUT-01') {
    return await request(`/api/v1/playout/playlist?channelId=${encodeURIComponent(channelId)}`);
  }
};

export function getMediaUrl(path) {
  if (!path) return '';
  if (path.startsWith('http://') || path.startsWith('https://')) return path;
  const apiBase = getApiBase();
  const apiKey = getApiKey();
  return `${apiBase}/api/v1/media/serve?path=${encodeURIComponent(path)}&apiKey=${encodeURIComponent(apiKey)}`;
}

export function getCgFrameUrl(bus = 'preview', timestamp = null) {
  const apiBase = getApiBase();
  const apiKey = getApiKey();
  const isProgram = String(bus).toUpperCase() === 'PROGRAM';
  const endpoint = isProgram ? '/api/v1/cg-controller/program-frame' : '/api/v1/cg-controller/preview-frame';
  const ts = timestamp || Date.now();
  return `${apiBase}${endpoint}?t=${ts}&apiKey=${encodeURIComponent(apiKey)}`;
}


