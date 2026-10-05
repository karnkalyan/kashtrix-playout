// Kashtrix Playout Web API Client - Communicates with Kashtrix.ApiGateway
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

export const PlayoutApi = {
  // Check backend health
  async checkHealth() {
    return await request('/api/v1/health');
  },

  // Read all live channel statuses from PlatformControlBus
  async getChannels() {
    return await request('/api/v1/playout/channels');
  },

  // Read specific channel status
  async getChannelStatus(channelId) {
    return await request(`/api/v1/playout/channels/${encodeURIComponent(channelId)}/status`);
  },

  // Read actual live playlist items from BroadcastDatabase
  async getPlaylist(channelId) {
    if (channelId) {
      const res = await request(`/api/v1/playout/channels/${encodeURIComponent(channelId)}/playlist`);
      if (res.ok) return res;
    }
    return await request('/api/v1/playout/playlist');
  },

  // Execute playout command (take, next, pause, resume, stop, reload_playlist, output_arm)
  async sendCommand(channelId, action, params = {}) {
    const payload = {
      channelId,
      action,
      params,
      ...params
    };
    return await request('/api/v1/playout/commands', {
      method: 'POST',
      body: payload
    });
  },

  // Convenience helper: Take item to air
  async take(channelId, itemId) {
    return await this.sendCommand(channelId, 'take', { itemId });
  },

  // Convenience helper: Take next item
  async takeNext(channelId) {
    return await this.sendCommand(channelId, 'next');
  },

  // Convenience helper: Freeze / Pause
  async pause(channelId) {
    return await this.sendCommand(channelId, 'pause');
  },

  // Convenience helper: Resume
  async resume(channelId) {
    return await this.sendCommand(channelId, 'resume');
  },

  // Convenience helper: Stop
  async stop(channelId) {
    return await this.sendCommand(channelId, 'stop');
  },

  // Convenience helper: Reload from database
  async reloadPlaylist(channelId) {
    return await this.sendCommand(channelId, 'reload_playlist');
  },

  // Convenience helper: Arm/Inhibit SDI / NDI output
  async armOutput(channelId, armed) {
    return await this.sendCommand(channelId, 'output_arm', { armed: String(armed) });
  },

  // Read NRCS rundowns
  async getRundowns() {
    return await request('/api/v1/nrcs/rundowns');
  },

  // Publish NRCS rundown to playout
  async publishRundown(rundownId, publishMos = true) {
    return await request(`/api/v1/nrcs/rundowns/${encodeURIComponent(rundownId)}/publish`, {
      method: 'POST',
      body: { publishMos }
    });
  },

  // Seed demo data into broadcast engine if empty
  async seedDemo() {
    return await request('/api/v1/demo/load', {
      method: 'POST'
    });
  }
};

// Build a URL that streams a media file through the API Gateway
export function getMediaUrl(filePath) {
  if (!filePath) return null;
  const base = getApiBase();
  const apiKey = getApiKey();
  return `${base}/api/v1/media/serve?path=${encodeURIComponent(filePath)}&apiKey=${encodeURIComponent(apiKey)}`;
}
