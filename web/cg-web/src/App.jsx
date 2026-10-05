import React, { useState, useEffect, useRef, useMemo } from 'react';
import {
  Layers, Tv, Eye, Play, Square, Pause, RefreshCw, AlertTriangle,
  Zap, Clock, Sparkles, Sliders, Type, Palette, ShieldAlert,
  CheckCircle2, Plus, Trash2, ArrowRight, Monitor, Radio, Compass,
  Calendar, Award, Server, Settings, Wifi, CheckSquare, XCircle, Flame,
  Search, ChevronRight, Upload, FolderOpen, FileCode, Check, Grid as GridIcon,
  List as ListIcon, Scissors, ArrowUpRight, Cast, Volume2, HelpCircle, HardDrive
} from 'lucide-react';
import { CgApi, getApiBase, setApiBase, getApiKey, getMediaUrl, getCgFrameUrl } from './api';

// Convert API templates to standardized UI model
function mapApiTemplates(data) {
  if (!Array.isArray(data)) return [];
  return data.map(item => {
    if (!item) return null;
    const itemName = item.name || item.Name || 'Untitled CG Template';
    const itemId = item.id !== undefined ? String(item.id) : (item.Id !== undefined ? String(item.Id) : itemName);
    const itemCategory = item.category || item.Category || '';
    const rawLayers = item.layers || item.Layers || [];

    const layers = Array.isArray(rawLayers) ? rawLayers.map((l, idx) => ({
      id: l?.id || l?.Id || `layer-${idx}`,
      name: l?.name || l?.Name || `Layer ${idx + 1}`,
      type: l?.type || l?.Type || 'Text',
      text: l?.text || l?.Text || '',
      x: l?.x !== undefined ? l.x : (l?.left || l?.Left || 0),
      y: l?.y !== undefined ? l.y : (l?.top || l?.Top || 0),
      width: l?.width || l?.Width || 0,
      height: l?.height || l?.Height || 0,
      fill: l?.fill || l?.Fill || '#FFFFFFFF',
      background: l?.background || l?.Background || '#00000000',
      fontFamily: l?.fontFamily || l?.FontFamily || 'Segoe UI',
      fontSize: l?.fontSize || l?.FontSize || 32,
      startSeconds: l?.startSeconds || l?.StartSeconds || 0,
      durationSeconds: l?.durationSeconds || l?.DurationSeconds || 8,
      visible: l?.visible !== undefined ? l.visible : true,
      dataField: l?.dataField || l?.DataField || ''
    })) : [];

    // Extract primary text layers for quick edits
    const textLayers = layers.filter(l => l && (l.type === 'Text' || l.type === 'Ticker' || l.type === 'Roll'));
    const l1 = textLayers.length > 0 ? (textLayers[0].text || itemName) : itemName;
    const l2 = textLayers.length > 1 ? (textLayers[1].text || '') : '';

    const rawCategory = (itemCategory || item.category || item.Category || '').trim();
    const category = rawCategory || (item.isImported || item.IsImported ? 'IMPORTED' : 'GENERAL');

    const badge = category;
    const catUpper = category.toUpperCase();
    const badgeColor =
      catUpper.includes('BREAKING') ? '#DC2626' :
      catUpper.includes('SPORT') ? '#EA580C' :
      catUpper.includes('WEATHER') ? '#0D9488' :
      catUpper.includes('MUSIC') ? '#DB2777' :
      catUpper.includes('FINANCE') ? '#10B981' :
      catUpper.includes('TICKER') ? '#D97706' :
      catUpper.includes('IMPORTED') ? '#8B5CF6' : '#6366F1';

    const thumbnailUrl = item.thumbnailUrl || `/api/v1/cg/thumbnail?name=${encodeURIComponent(itemName)}`;
    const previewUrl = item.previewUrl || `/api/v1/cg/thumbnail?name=${encodeURIComponent(itemName)}&w=1280&h=720`;

    return {
      id: itemId,
      name: itemName,
      category,
      badge,
      badgeColor,
      line1: l1 || itemName,
      line2: l2 || '',
      autoOut: item.durationSeconds || item.DurationSeconds || 8,
      externalLayer: item.externalLayer !== undefined ? item.externalLayer : (item.ExternalLayer !== undefined ? item.ExternalLayer : 1),
      isLive: false,
      thumbnailUrl,
      previewUrl,
      layers: layers
    };
  }).filter(Boolean);
}

// Strict 16:9 Broadcast Graphic Monitor for Preview and Program viewports
function GraphicMonitor({
  graphic,
  busName = 'PREVIEW',
  isLive = false,
  showSafeGuides = true,
  onToggleSafeGuides = null,
  transition = 'NONE',
  transitionProgress = 1,
  customEdits = null,
  channelStatus = null,
  activeVideoUrl = null
}) {
  const apiBase = getApiBase();
  const apiKey = getApiKey();

  // Live video frame polling from Kashtrix Playout / CG Controller shared memory bridge
  const [liveFrameSrc, setLiveFrameSrc] = useState(null);
  const [hasLiveSignal, setHasLiveSignal] = useState(false);

  useEffect(() => {
    let active = true;
    let inFlight = false;
    const bus = isLive ? 'PROGRAM' : 'PREVIEW';
    const intervalMs = 60; // Smooth 16 fps frame rate without buffer congestion

    const poll = () => {
      if (!active || inFlight) return;
      inFlight = true;
      const url = getCgFrameUrl(bus, Date.now());
      const img = new Image();
      img.onload = () => {
        inFlight = false;
        if (!active) return;
        setLiveFrameSrc(url);
        setHasLiveSignal(true);
        setTimeout(poll, intervalMs);
      };
      img.onerror = () => {
        inFlight = false;
        if (!active) return;
        setHasLiveSignal(false);
        setTimeout(poll, 800);
      };
      img.src = url;
    };

    poll();
    return () => { active = false; };
  }, [isLive, apiBase, apiKey]);

  // URL for high-resolution PNG render from CgThumbnailRenderer / CgCompositor
  const renderUrl = graphic?.name
    ? `${apiBase}/api/v1/cg/thumbnail?name=${encodeURIComponent(graphic.name)}&w=960&h=540&apiKey=${encodeURIComponent(apiKey)}`
    : null;

  // Transition style for Program or Switcher animation
  const transitionStyle = useMemo(() => {
    if (transition === 'FADE') {
      return { opacity: transitionProgress, transition: 'opacity 0.25s ease-in-out' };
    }
    if (transition === 'WIPE LEFT') {
      return { clipPath: `inset(0 ${(1 - transitionProgress) * 100}% 0 0)` };
    }
    if (transition === 'WIPE RIGHT') {
      return { clipPath: `inset(0 0 0 ${(1 - transitionProgress) * 100}%)` };
    }
    if (transition === 'PUSH UP') {
      return { transform: `translateY(${(1 - transitionProgress) * 100}%)` };
    }
    if (transition === 'ZOOM') {
      return { transform: `scale(${0.85 + 0.15 * transitionProgress})`, opacity: transitionProgress };
    }
    return {};
  }, [transition, transitionProgress]);

  return (
    <div style={{
      position: 'relative',
      width: '100%',
      height: '100%',
      background: '#04070A',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      overflow: 'hidden',
      padding: '4px'
    }}>
      {/* Strict 16:9 Aspect Ratio Frame fitting container height and width (same geometry as CgController & playout-web) */}
      <div style={{
        position: 'relative',
        height: '100%',
        maxHeight: '100%',
        maxWidth: '100%',
        aspectRatio: '16 / 9',
        width: 'auto',
        background: '#000000',
        border: `1px solid ${isLive ? '#6B3036' : '#47305F'}`,
        boxShadow: isLive ? '0 0 20px rgba(239, 68, 68, 0.3)' : '0 0 14px rgba(110, 69, 161, 0.25)',
        borderRadius: '3px',
        overflow: 'hidden',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center'
      }}>
        {/* Layer 0: Base Video / Broadcast Background (Matching CgController _latestPlayoutProgramFrame) */}
        {activeVideoUrl ? (
          <video
            src={activeVideoUrl}
            autoPlay
            loop
            muted
            playsInline
            style={{
              position: 'absolute',
              inset: 0,
              width: '100%',
              height: '100%',
              objectFit: 'contain',
              background: '#000000'
            }}
          />
        ) : (
          <div style={{
            position: 'absolute',
            inset: 0,
            background: isLive
              ? 'radial-gradient(ellipse at center, #110B12 0%, #050608 100%)'
              : 'radial-gradient(ellipse at center, #0F0E18 0%, #050608 100%)'
          }}>
            {/* Subtle broadcast cross-hatch or vignette */}
            <div style={{
              position: 'absolute',
              inset: 0,
              opacity: 0.15,
              backgroundImage: 'linear-gradient(45deg, #1A2230 25%, transparent 25%), linear-gradient(-45deg, #1A2230 25%, transparent 25%), linear-gradient(45deg, transparent 75%, #1A2230 75%), linear-gradient(-45deg, transparent 75%, #1A2230 75%)',
              backgroundSize: '24px 24px',
              backgroundPosition: '0 0, 0 12px, 12px -12px, -12px 0px'
            }} />
          </div>
        )}

        {/* Layer 1: Live Output Video Frame from Shared Memory Bridge or Composited Template */}
        {liveFrameSrc ? (
          <div style={{
            position: 'absolute',
            inset: 0,
            width: '100%',
            height: '100%',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            ...transitionStyle
          }}>
            <img
              src={liveFrameSrc}
              alt={`Live CG ${busName}`}
              style={{
                width: '100%',
                height: '100%',
                objectFit: 'contain',
                display: 'block'
              }}
            />
          </div>
        ) : renderUrl ? (
          <div style={{
            position: 'absolute',
            inset: 0,
            width: '100%',
            height: '100%',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            ...transitionStyle
          }}>
            <img
              src={renderUrl}
              alt={graphic.name}
              style={{
                width: '100%',
                height: '100%',
                objectFit: 'contain',
                display: 'block'
              }}
            />
          </div>
        ) : !activeVideoUrl ? (
          /* Standby clear plate */
          <div style={{
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            justifyContent: 'center',
            gap: '8px',
            color: isLive ? 'rgba(239,68,68,0.4)' : 'rgba(200,155,237,0.4)',
            zIndex: 10
          }}>
            <Tv size={36} opacity={0.6} />
            <span style={{ fontSize: '11px', fontWeight: '800', letterSpacing: '0.08em', color: isLive ? '#EF4444' : '#C89BED' }}>
              {isLive ? 'PROGRAM BUS · STANDBY BLACK' : 'PREVIEW BUS · STANDBY'}
            </span>
            <span style={{ fontSize: '9.5px', color: '#64748B' }}>
              {isLive ? 'Select a graphic template and click TAKE or PLAY PROGRAM' : 'Select a graphic template and click PLAY PREVIEW or CUE'}
            </span>
          </div>
        ) : null}

        {/* Action/Title Safe Border Guides matching CgControllerWindow.xaml lines 31 & 54 */}
        {showSafeGuides && (
          <div style={{
            position: 'absolute',
            inset: 0,
            pointerEvents: 'none',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            zIndex: 25
          }}>
            {/* Action Safe (90%) */}
            <div style={{
              position: 'absolute',
              inset: '5%',
              border: `1px dashed ${isLive ? 'rgba(239, 68, 68, 0.4)' : 'rgba(168, 85, 247, 0.4)'}`,
              borderRadius: '2px'
            }}>
              {/* Title Safe (80%) */}
              <div style={{
                position: 'absolute',
                inset: '5%',
                border: `1px solid ${isLive ? 'rgba(239, 68, 68, 0.55)' : 'rgba(168, 85, 247, 0.55)'}`,
                borderRadius: '2px'
              }} />
            </div>
            {/* Center crosshair */}
            <div style={{ position: 'absolute', width: '14px', height: '1px', background: 'rgba(255,255,255,0.25)' }} />
            <div style={{ position: 'absolute', height: '14px', width: '1px', background: 'rgba(255,255,255,0.25)' }} />
          </div>
        )}

        {/* Broadcast Overlay Badges */}
        <div style={{
          position: 'absolute',
          top: '6px',
          left: '6px',
          display: 'flex',
          alignItems: 'center',
          gap: '5px',
          background: isLive ? '#7F1D1D' : '#3B185F',
          border: `1px solid ${isLive ? '#EF4444' : '#8D55B7'}`,
          padding: '2px 7px',
          borderRadius: '3px',
          fontSize: '9.5px',
          fontWeight: '900',
          color: '#FFFFFF',
          letterSpacing: '0.06em',
          zIndex: 30
        }}>
          <span style={{
            width: '6px',
            height: '6px',
            borderRadius: '50%',
            background: isLive ? '#EF4444' : '#C89BED',
            boxShadow: `0 0 6px ${isLive ? '#EF4444' : '#C89BED'}`
          }} />
          <span>{isLive ? 'ON AIR · PROGRAM' : 'PREVIEW'}</span>
          {hasLiveSignal && (
            <span style={{ fontSize: '8px', opacity: 0.85, color: '#A7F3D0', marginLeft: '2px', letterSpacing: '0.04em' }}>· LIVE FEED</span>
          )}
        </div>

        <div style={{
          position: 'absolute',
          top: '6px',
          right: '6px',
          display: 'flex',
          alignItems: 'center',
          gap: '6px',
          background: 'rgba(0,0,0,0.65)',
          border: '1px solid #1E293B',
          padding: '2px 6px',
          borderRadius: '3px',
          fontSize: '9px',
          fontFamily: 'var(--font-mono, monospace)',
          fontWeight: '700',
          color: '#94A3B8',
          zIndex: 30
        }}>
          <span>1080p50 · 16:9</span>
          {onToggleSafeGuides && (
            <button
              onClick={e => { e.stopPropagation(); onToggleSafeGuides(); }}
              style={{
                background: showSafeGuides ? 'rgba(110, 69, 161, 0.4)' : 'transparent',
                border: '1px solid #334155',
                color: showSafeGuides ? '#C89BED' : '#64748B',
                fontSize: '8px',
                padding: '1px 4px',
                borderRadius: '2px',
                cursor: 'pointer'
              }}
              title="Toggle Broadcast Safe Title & Action Guides"
            >
              SAFE
            </button>
          )}
        </div>

        {graphic?.name && (
          <div style={{
            position: 'absolute',
            bottom: '6px',
            left: '6px',
            background: 'rgba(0,0,0,0.75)',
            border: '1px solid #1E293B',
            padding: '2px 8px',
            borderRadius: '3px',
            fontSize: '9.5px',
            fontWeight: '700',
            color: '#E2E8F0',
            maxWidth: '70%',
            overflow: 'hidden',
            textOverflow: 'ellipsis',
            whiteSpace: 'nowrap',
            zIndex: 30
          }}>
            {graphic.name}
          </div>
        )}
      </div>
    </div>
  );
}

export default function CGControllerApp() {
  // Master API State
  const [templates, setTemplates] = useState([]);
  const [selectedTemplateId, setSelectedTemplateId] = useState('');
  const [activeCategory, setActiveCategory] = useState('ALL');
  const [searchQuery, setSearchQuery] = useState('');
  const [catalogViewMode, setCatalogViewMode] = useState('GRID'); // 'GRID' | 'LIST'
  const [showSafeGuides, setShowSafeGuides] = useState(true);

  // Clocks
  const [masterClock, setMasterClock] = useState('00:00:00:00');
  const [nepaliDate, setNepaliDate] = useState('२०८३ आश्विन १७ शनिबार');

  // Backend Connectivity
  const [isApiConnected, setIsApiConnected] = useState(false);
  const [apiLatency, setApiLatency] = useState(null);
  const [cgTargets, setCgTargets] = useState(['KTX-PLAYOUT-01']);
  const [selectedTarget, setSelectedTarget] = useState('KTX-PLAYOUT-01');
  const [selectedTargetsList, setSelectedTargetsList] = useState(['KTX-PLAYOUT-01']);
  const [apiBaseUrl, setApiBaseUrl] = useState(getApiBase());
  const [showConfigModal, setShowConfigModal] = useState(false);

  // Playout Confidence & Background Video Sync (matches desktop CgController & playout-web)
  const [playoutChannelStatus, setPlayoutChannelStatus] = useState(null);
  const [activePlayoutVideo, setActivePlayoutVideo] = useState(null);
  const [previewPlayoutVideo, setPreviewPlayoutVideo] = useState(null);

  // Visible Panels (PANELS menu toggle)
  const [visiblePanels, setVisiblePanels] = useState({
    preview: true,
    switcher: true,
    program: true,
    catalog: true,
    layers: true,
    targets: true
  });
  const [showPanelsMenu, setShowPanelsMenu] = useState(false);

  // File Import & Seed Status
  const fileInputRef = useRef(null);
  const [importStatus, setImportStatus] = useState(null);

  // Lower Center Panel Tabs: 'LAYERS' | 'OVERLAYS' | 'ONAIR' | 'QUICKEDIT' | 'SCHEDULER'
  const [centerTab, setCenterTab] = useState('LAYERS');
  const [rightTab, setRightTab] = useState('TARGETS'); // 'TARGETS' | 'OUTPUT' | 'STATUS'

  // Live On-Air Stack from API
  const [onAirItems, setOnAirItems] = useState([]);
  const [selectedOnAirKeys, setSelectedOnAirKeys] = useState(new Set());

  // Independent Buses: Preview vs Program
  const [previewGraphic, setPreviewGraphic] = useState(null);
  const [programGraphic, setProgramGraphic] = useState(null);
  const lastLocalActionTime = useRef(0);

  // Switcher Settings
  const [takeTransition, setTakeTransition] = useState('CUT'); // 'CUT' | 'FADE' | 'WIPE LEFT' | 'WIPE RIGHT' | 'PUSH UP' | 'ZOOM'
  const [takeDurationSeconds, setTakeDurationSeconds] = useState(0.45);
  const [transitionProgress, setTransitionProgress] = useState(1);
  const [isTransitioning, setIsTransitioning] = useState(false);

  // Selected Template & Layer Inspector
  const selectedTemplate = useMemo(() => {
    return templates.find(t => t.id === selectedTemplateId) || (templates.length > 0 ? templates[0] : null);
  }, [templates, selectedTemplateId]);

  const [selectedLayerIndex, setSelectedLayerIndex] = useState(0);

  // Contextual Quick Edit State
  const [editBadge, setEditBadge] = useState('LIVE');
  const [editLine1, setEditLine1] = useState('');
  const [editLine2, setEditLine2] = useState('');
  const [editColor, setEditColor] = useState('#EF4444');
  const [editBgColor, setEditBgColor] = useState('#0F172A');
  const [editFontSize, setEditFontSize] = useState(32);
  const [editTargetLayer, setEditTargetLayer] = useState(1);

  // Multi-Overlay Manager
  const [activeOverlayLayers, setActiveOverlayLayers] = useState([0, 1, 2, 3]);
  const [selectedOverlayChannel, setSelectedOverlayChannel] = useState(3);

  // Broadcast Output / DeckLink / NDI Settings
  const [enableNdiAlpha, setEnableNdiAlpha] = useState(true);
  const [ndiSourceName, setNdiSourceName] = useState('KASHTRIX-CG-ALPHA');
  const [enableDeckLink, setEnableDeckLink] = useState(false);
  const [deckLinkCard, setDeckLinkCard] = useState('DeckLink 8K Pro (1)');
  const [deckLinkKeyerMode, setDeckLinkKeyerMode] = useState('Internal');
  const [deckLinkKeyLevel, setDeckLinkKeyLevel] = useState(255);
  const [outputPreset, setOutputPreset] = useState('1080p50');
  const [prerollFrames, setPrerollFrames] = useState(3);
  const [lastRouteOperation, setLastRouteOperation] = useState({ name: 'READY', time: 'SYSTEM INITIALIZED', detail: 'Controller online and connected to ApiGateway' });

  // Ticker State
  const [tickerItems, setTickerItems] = useState([
    '⚡ KASHTRIX BROADCAST GRAPHICS ENGINE ONLINE',
    'REALTIME CG LAYER CONTROLLER ACTIVE · BROADCAST SAFE 1080p50'
  ]);
  const [newTickerText, setNewTickerText] = useState('');
  const [tickerSpeed, setTickerSpeed] = useState(28);
  const [isTickerEmergency, setIsTickerEmergency] = useState(false);

  // Dynamic CG Scheduler
  const [schedules, setSchedules] = useState([]);
  const [newSchedProject, setNewSchedProject] = useState('');
  const [newSchedStart, setNewSchedStart] = useState('21:00:00');
  const [newSchedStop, setNewSchedStop] = useState('22:00:00');
  const [newSchedLayer, setNewSchedLayer] = useState(3);
  const [newSchedDays, setNewSchedDays] = useState('Everyday');

  // Master Digital Clock
  useEffect(() => {
    const clockInterval = setInterval(() => {
      const now = new Date();
      const h = String(now.getHours()).padStart(2, '0');
      const m = String(now.getMinutes()).padStart(2, '0');
      const s = String(now.getSeconds()).padStart(2, '0');
      const f = String(Math.floor((now.getMilliseconds() / 1000) * 25)).padStart(2, '0');
      setMasterClock(`${h}:${m}:${s}:${f}`);
    }, 40);
    return () => clearInterval(clockInterval);
  }, []);

  // Poll Real Kashtrix CG API (Health, Targets, Templates, On-Air)
  useEffect(() => {
    let isSubscribed = true;

    const pollApi = async () => {
      const startTime = performance.now();
      try {
        const health = await CgApi.checkHealth();
        if (!isSubscribed) return;

        if (health && health.ok) {
          setIsApiConnected(true);
          setApiLatency(Math.round(performance.now() - startTime));

          // Fetch CG Targets
          try {
            const targetsRes = await CgApi.getTargets();
            if (isSubscribed && targetsRes && targetsRes.ok && Array.isArray(targetsRes.data) && targetsRes.data.length > 0) {
              const channelIds = targetsRes.data.map(t => t.channelId || t.ChannelId);
              setCgTargets(channelIds);
              if (!channelIds.includes(selectedTarget)) {
                setSelectedTarget(channelIds[0]);
              }
            }
          } catch { }

          // Fetch Real CG Templates & Items from Database / Factory
          try {
            const tplRes = await CgApi.getTemplates();
            if (isSubscribed && tplRes && tplRes.ok && Array.isArray(tplRes.data)) {
              const mapped = mapApiTemplates(tplRes.data);
              setTemplates(mapped);
              if (mapped.length > 0) {
                setSelectedTemplateId(prev => prev && mapped.some(m => m.id === prev) ? prev : mapped[0].id);
                if (!previewGraphic) setPreviewGraphic(mapped[0]);
              }
            }
          } catch { }

          // Fetch Real On-Air items active on PROGRAM
          try {
            const onAirRes = await CgApi.getOnAir(selectedTarget);
            if (isSubscribed && onAirRes && onAirRes.ok && Array.isArray(onAirRes.data)) {
              setOnAirItems(onAirRes.data);
              if (onAirRes.data.length > 0) {
                const live = onAirRes.data[0];
                const matched = templates.find(t => t.name === live.name || t.id === live.projectId);
                setProgramGraphic(prev => {
                  if (prev && (prev.name === live.name || prev.id === live.projectId)) return prev;
                  return matched || {
                    id: live.projectId,
                    name: live.name,
                    category: live.category || 'LIVE',
                    line1: live.name,
                    line2: `Active on ${live.channelId} · Layer ${live.layer}`,
                    badge: 'LIVE',
                    badgeColor: '#EF4444'
                  };
                });
              } else {
                if (Date.now() - lastLocalActionTime.current > 3000) {
                  setProgramGraphic(null);
                }
              }
            }
          } catch { }

          // Playout Confidence Video Sync (matching CgController _latestPlayoutProgramFrame)
          try {
            const chRes = await CgApi.getPlayoutChannels();
            if (isSubscribed && chRes && chRes.ok && Array.isArray(chRes.data) && chRes.data.length > 0) {
              const currentCh = chRes.data.find(c => c.channelId === selectedTarget) || chRes.data[0];
              setPlayoutChannelStatus(currentCh);
              if (currentCh) {
                const plRes = await CgApi.getPlayoutPlaylist(currentCh.channelId);
                const plData = plRes?.data || plRes;
                if (isSubscribed && Array.isArray(plData) && plData.length > 0) {
                  const onAir = plData.find(p => p.status === 'ON_AIR' || p.Status === 'ON_AIR') || plData[0];
                  const cued = plData.find(p => p.status === 'CUED' || p.Status === 'CUED') || plData[1] || onAir;
                  const src = onAir?.source || onAir?.Source;
                  if (src) {
                    setActivePlayoutVideo(getMediaUrl(src));
                  }
                  const cuedSrc = cued?.source || cued?.Source;
                  if (cuedSrc) {
                    setPreviewPlayoutVideo(getMediaUrl(cuedSrc));
                  }
                }
              }
            }
          } catch { }
        } else {
          setIsApiConnected(false);
        }
      } catch (err) {
        if (isSubscribed) {
          setIsApiConnected(false);
        }
      }
    };

    pollApi();
    const interval = setInterval(pollApi, 1200);

    return () => {
      isSubscribed = false;
      clearInterval(interval);
    };
  }, [selectedTarget, templates]);

  // Synchronize inspector fields when selecting a template
  useEffect(() => {
    if (selectedTemplate) {
      setEditBadge(selectedTemplate.badge || 'LIVE');
      setEditLine1(selectedTemplate.line1 || '');
      setEditLine2(selectedTemplate.line2 || '');
      setEditColor(selectedTemplate.badgeColor || '#EF4444');
      setEditBgColor('#0F172A');
      setEditFontSize(32);
      setEditTargetLayer(selectedTemplate.externalLayer || 1);
    }
  }, [selectedTemplateId, templates]);

  // --- API Action Dispatches ---

  // Play Preview: Load selected template onto PREVIEW bus
  const handlePlayPreview = async (templateToPlay = null) => {
    const target = templateToPlay || selectedTemplate;
    if (!target) return;

    setPreviewGraphic(target);
    setLastRouteOperation({
      name: 'PREVIEW PLAY',
      time: new Date().toLocaleTimeString(),
      detail: `Cued "${target.name}" to PREVIEW bus on channel ${selectedTarget}`
    });

    try {
      await CgApi.playPreview(selectedTarget, target.name, target.externalLayer || 1, {
        Headline: target.line1,
        Subtitle: target.line2,
        Badge: target.badge,
        fill: target.badgeColor
      });
    } catch { }
  };

  // Stop Preview
  const handleStopPreview = async () => {
    setPreviewGraphic(null);
    setLastRouteOperation({
      name: 'PREVIEW STOP',
      time: new Date().toLocaleTimeString(),
      detail: 'Stopped and cleared Preview bus'
    });
    try {
      await CgApi.stop(selectedTarget, -1, 'PREVIEW');
    } catch { }
  };

  // Clear Preview
  const handleClearPreview = async () => {
    setPreviewGraphic(null);
    setLastRouteOperation({
      name: 'PREVIEW CLEAR',
      time: new Date().toLocaleTimeString(),
      detail: 'Instant cut clear on Preview bus'
    });
    try {
      await CgApi.clear(selectedTarget, -1, 'PREVIEW');
    } catch { }
  };

  // Play Program: Take selected template directly onto PROGRAM bus
  const handlePlayProgram = async (templateToPlay = null) => {
    lastLocalActionTime.current = Date.now();
    const target = templateToPlay || selectedTemplate;
    if (!target) return;

    setProgramGraphic(target);
    setLastRouteOperation({
      name: 'PROGRAM PLAY',
      time: new Date().toLocaleTimeString(),
      detail: `Direct play of "${target.name}" to PROGRAM on channel ${selectedTarget}`
    });

    try {
      await CgApi.play(selectedTarget, target.name, target.externalLayer || 1, 'PROGRAM', {
        Headline: editLine1 || target.line1,
        Subtitle: editLine2 || target.line2,
        Badge: editBadge || target.badge,
        fill: editColor || target.badgeColor,
        background: editBgColor,
        fontSize: String(editFontSize)
      });
    } catch { }
  };

  // Stop Program (Graceful Out-Animation)
  const handleStopProgram = async () => {
    lastLocalActionTime.current = Date.now();
    setLastRouteOperation({
      name: 'PROGRAM STOP',
      time: new Date().toLocaleTimeString(),
      detail: `Animated out on-air graphics on channel ${selectedTarget}`
    });
    try {
      await CgApi.stop(selectedTarget, -1, 'PROGRAM');
      setProgramGraphic(null);
    } catch { }
  };

  // Clear Program (Instant Cut)
  const handleClearProgram = async () => {
    lastLocalActionTime.current = Date.now();
    setLastRouteOperation({
      name: 'PROGRAM CLEAR',
      time: new Date().toLocaleTimeString(),
      detail: `Immediate cut clear on Program bus on channel ${selectedTarget}`
    });
    try {
      await CgApi.clear(selectedTarget, -1, 'PROGRAM');
      setProgramGraphic(null);
    } catch { }
  };

  // Update Live Text (Hot update without re-triggering animation)
  const handleUpdateLiveText = async () => {
    if (!selectedTemplate) return;
    setLastRouteOperation({
      name: 'HOT UPDATE',
      time: new Date().toLocaleTimeString(),
      detail: `Hot-updated text fields for "${selectedTemplate.name}" on ${selectedTarget}`
    });

    try {
      await CgApi.update(selectedTarget, selectedTemplate.name, editTargetLayer, 'PROGRAM', {
        Headline: editLine1,
        Subtitle: editLine2,
        Badge: editBadge,
        fill: editColor,
        background: editBgColor,
        fontSize: String(editFontSize)
      });

      setProgramGraphic(prev => prev ? {
        ...prev,
        line1: editLine1,
        line2: editLine2,
        badge: editBadge,
        badgeColor: editColor,
        bgColor: editBgColor,
        fontSize: editFontSize
      } : prev);
    } catch { }
  };

  // Switcher Transition Execution: TAKE / AUTO / CUT
  const executeTake = async (transitionMode = 'CUT') => {
    if (!previewGraphic) return;
    lastLocalActionTime.current = Date.now();

    setIsTransitioning(true);
    setTransitionProgress(0);

    const dur = transitionMode === 'CUT' ? 0 : (takeDurationSeconds * 1000);

    if (dur > 0) {
      const startTime = performance.now();
      const animInterval = setInterval(() => {
        const elapsed = performance.now() - startTime;
        const p = Math.min(elapsed / dur, 1);
        setTransitionProgress(p);
        if (p >= 1) {
          clearInterval(animInterval);
          setIsTransitioning(false);
          setProgramGraphic(previewGraphic);
        }
      }, 16);
    } else {
      setTransitionProgress(1);
      setIsTransitioning(false);
      setProgramGraphic(previewGraphic);
    }

    setLastRouteOperation({
      name: `SWITCHER ${transitionMode}`,
      time: new Date().toLocaleTimeString(),
      detail: `Took Preview to Program using ${transitionMode} (${takeDurationSeconds}s)`
    });

    try {
      await CgApi.take(selectedTarget, previewGraphic.externalLayer || 1, transitionMode, previewGraphic.name);
    } catch { }
  };

  // Seed Canonical Demo CG Templates from Backend
  const handleSeedDemo = async () => {
    try {
      setImportStatus({ loading: true, message: 'Loading canonical broadcast CG templates...' });
      const res = await CgApi.seedDemo();
      if (res && res.ok === false) {
        setImportStatus({ error: true, message: res.message || res.error || 'Failed to seed demo CG' });
        setTimeout(() => setImportStatus(null), 5000);
        return;
      }
      const tplRes = await CgApi.getTemplates();
      if (tplRes && tplRes.ok && Array.isArray(tplRes.data)) {
        const mapped = mapApiTemplates(tplRes.data);
        setTemplates(mapped);
        if (mapped.length > 0) {
          setSelectedTemplateId(mapped[0].id);
          setPreviewGraphic(mapped[0]);
        }
      }
      setImportStatus({ success: true, message: 'Canonical broadcast CG templates loaded!' });
      setTimeout(() => setImportStatus(null), 3500);
    } catch (err) {
      setImportStatus({ error: true, message: err.message || 'Error seeding demo CG' });
      setTimeout(() => setImportStatus(null), 5000);
    }
  };

  // Import CG Template / Project (.kcg, .json)
  const handleImportFile = async (e) => {
    const file = e.target.files?.[0];
    if (!file) return;
    try {
      setImportStatus({ loading: true, message: `Importing ${file.name}...` });
      const isJson = file.name.toLowerCase().endsWith('.json');
      const reader = new FileReader();

      reader.onload = async (event) => {
        try {
          const rawName = file.name.replace(/\.[^/.]+$/, "");
          let payload = { name: rawName };
          if (isJson) {
            payload.json = event.target.result;
          } else {
            const dataUrl = event.target.result;
            const base64 = dataUrl.split(',')[1] || dataUrl;
            payload.base64 = base64;
          }

          const res = await CgApi.importCg(payload);
          if (res && res.ok) {
            setImportStatus({ success: true, message: `Imported "${rawName}" successfully!` });
            setTimeout(() => setImportStatus(null), 4000);

            const tplRes = await CgApi.getTemplates();
            if (tplRes && tplRes.ok && Array.isArray(tplRes.data)) {
              const mapped = mapApiTemplates(tplRes.data);
              setTemplates(mapped);
              const found = mapped.find(m => m.name.toLowerCase() === rawName.toLowerCase());
              if (found) {
                setSelectedTemplateId(found.id);
                setPreviewGraphic(found);
              }
            }
          } else {
            setImportStatus({ error: true, message: res?.message || 'Import failed' });
            setTimeout(() => setImportStatus(null), 5000);
          }
        } catch (err) {
          setImportStatus({ error: true, message: err.message || 'Import failed' });
          setTimeout(() => setImportStatus(null), 5000);
        }
      };

      if (isJson) reader.readAsText(file);
      else reader.readAsDataURL(file);
    } catch (err) {
      setImportStatus({ error: true, message: err.message });
      setTimeout(() => setImportStatus(null), 5000);
    } finally {
      if (e.target) e.target.value = '';
    }
  };

  // Stop Individual On-Air Item
  const handleStopOnAirItem = async (item, instant = false) => {
    const fn = instant ? CgApi.clear : CgApi.stop;
    await fn(item.channelId || selectedTarget, {
      projectId: item.projectId,
      name: item.name,
      template: item.name,
      layer: item.layer !== undefined ? item.layer : -1,
      bus: 'PROGRAM'
    });
    setOnAirItems(prev => prev.filter(x => !(x.projectId === item.projectId && x.layer === item.layer)));
  };

  // Add Item to Live Ticker Wire
  const handleAddTicker = (e) => {
    e.preventDefault();
    if (!newTickerText.trim()) return;
    setTickerItems(prev => [...prev, newTickerText.trim()]);
    setNewTickerText('');
  };

  // Categories extracted from real API templates
  const availableCategories = useMemo(() => {
    return ['ALL', ...Array.from(new Set(templates.map(t => t.category).filter(Boolean))).sort()];
  }, [templates]);

  // Filtered Templates by Search & Category
  const filteredTemplates = useMemo(() => {
    return templates.filter(t => {
      const matchesCat = activeCategory === 'ALL' || t.category === activeCategory;
      const matchesSearch = !searchQuery.trim() ||
        t.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
        t.category.toLowerCase().includes(searchQuery.toLowerCase()) ||
        (t.line1 && t.line1.toLowerCase().includes(searchQuery.toLowerCase())) ||
        (t.line2 && t.line2.toLowerCase().includes(searchQuery.toLowerCase()));
      return matchesCat && matchesSearch;
    });
  }, [templates, activeCategory, searchQuery]);

  return (
    <div style={{ display: 'flex', flexDirection: 'column', height: '100vh', background: '#06090E', color: '#F8FAFC', overflow: 'hidden' }}>
      {/* 1. TOP HEADER / TITLE BAR */}
      <header style={{
        height: '52px',
        backgroundColor: '#0E1117',
        borderBottom: '1px solid #1E293B',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        padding: '0 14px',
        zIndex: 20
      }}>
        {/* Branding & Subtitle */}
        <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
          <div style={{
            width: '32px',
            height: '32px',
            background: 'linear-gradient(135deg, #6E45A1, #31204A)',
            borderRadius: '6px',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            boxShadow: '0 0 12px rgba(110, 69, 161, 0.4)'
          }}>
            <Layers size={18} color="#FFFFFF" />
          </div>
          <div>
            <div style={{ fontSize: '14px', fontWeight: '800', letterSpacing: '0.06em', color: '#FFFFFF' }}>
              KASHTRIX <span style={{ color: '#C89BED' }}>CG CONTROLLER</span>
            </div>
            <div style={{ fontSize: '9.5px', color: '#94A3B8', letterSpacing: '0.03em' }}>
              Multi-overlay graphics playback · dynamic layers · live broadcast switcher
            </div>
          </div>
        </div>

        {/* Center / Right: PANELS Menu, Clock, Targets, Connect & Settings */}
        <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
          {/* PANELS Dropdown Menu */}
          <div style={{ position: 'relative' }}>
            <button
              onClick={() => setShowPanelsMenu(!showPanelsMenu)}
              style={{
                padding: '5px 10px',
                borderRadius: '4px',
                background: '#161F2E',
                border: '1px solid #2A364F',
                color: '#E2E8F0',
                fontSize: '11px',
                fontWeight: '700',
                display: 'flex',
                alignItems: 'center',
                gap: '5px',
                cursor: 'pointer'
              }}
            >
              PANELS ▼
            </button>

            {showPanelsMenu && (
              <div style={{
                position: 'absolute',
                top: '100%',
                left: 0,
                marginTop: '4px',
                background: '#0D131C',
                border: '1px solid #2B384E',
                borderRadius: '6px',
                boxShadow: '0 8px 24px rgba(0,0,0,0.8)',
                padding: '6px',
                minWidth: '170px',
                zIndex: 100,
                display: 'flex',
                flexDirection: 'column',
                gap: '2px'
              }}>
                {[
                  { key: 'preview', label: 'Preview Bus' },
                  { key: 'switcher', label: 'Switcher' },
                  { key: 'program', label: 'Program Bus' },
                  { key: 'catalog', label: 'CG Catalog' },
                  { key: 'layers', label: 'Layers & Edits' },
                  { key: 'targets', label: 'Targets / Output' }
                ].map(p => (
                  <label
                    key={p.key}
                    style={{
                      display: 'flex',
                      alignItems: 'center',
                      gap: '8px',
                      padding: '4px 8px',
                      borderRadius: '4px',
                      fontSize: '11px',
                      color: '#E2E8F0',
                      cursor: 'pointer',
                      background: visiblePanels[p.key] ? 'rgba(110, 69, 161, 0.15)' : 'transparent'
                    }}
                  >
                    <input
                      type="checkbox"
                      checked={visiblePanels[p.key]}
                      onChange={() => setVisiblePanels(prev => ({ ...prev, [p.key]: !prev[p.key] }))}
                    />
                    {p.label}
                  </label>
                ))}
                <div style={{ height: '1px', background: '#1E293B', margin: '4px 0' }} />
                <button
                  onClick={() => {
                    setVisiblePanels({ preview: true, switcher: true, program: true, catalog: true, layers: true, targets: true });
                    setShowPanelsMenu(false);
                  }}
                  style={{
                    padding: '5px 8px',
                    borderRadius: '4px',
                    background: '#1F2937',
                    color: '#A855F7',
                    fontSize: '10.5px',
                    fontWeight: '700',
                    textAlign: 'left',
                    border: 'none',
                    cursor: 'pointer'
                  }}
                >
                  Restore All Panels
                </button>
              </div>
            )}
          </div>

          {/* Master SMPTE Clock & BS Date */}
          <div style={{
            display: 'flex',
            alignItems: 'center',
            gap: '8px',
            background: '#0B0F17',
            border: '1px solid #1E293B',
            borderRadius: '6px',
            padding: '4px 12px'
          }}>
            <Calendar size={13} color="#38BDF8" />
            <span style={{ fontSize: '10.5px', color: '#94A3B8', fontWeight: '600' }}>
              {nepaliDate.split(' ')[0]}
            </span>
            <span style={{ height: '12px', width: '1px', background: '#334155' }} />
            <span className="smpte-code" style={{ fontSize: '14px', fontWeight: '800', color: '#C89BED' }}>
              {masterClock}
            </span>
          </div>

          {/* Target Channel Pill */}
          <div style={{ display: 'flex', alignItems: 'center', gap: '5px', background: '#0F172A', padding: '3px 8px', borderRadius: '5px', border: '1px solid #1E293B' }}>
            <span style={{ fontSize: '9.5px', color: '#64748B', fontWeight: '800' }}>TARGET:</span>
            <select
              value={selectedTarget}
              onChange={e => setSelectedTarget(e.target.value)}
              style={{
                background: 'transparent',
                border: 'none',
                color: '#38BDF8',
                fontSize: '11px',
                fontWeight: '800',
                outline: 'none',
                cursor: 'pointer'
              }}
            >
              {cgTargets.map(t => (
                <option key={t} value={t} style={{ background: '#0F172A', color: '#FFFFFF' }}>{t}</option>
              ))}
            </select>
          </div>

          {/* API Connectivity Badge */}
          <div
            onClick={() => setShowConfigModal(true)}
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: '6px',
              background: isApiConnected ? 'rgba(16, 185, 129, 0.12)' : 'rgba(245, 158, 11, 0.12)',
              border: `1px solid ${isApiConnected ? 'rgba(16, 185, 129, 0.3)' : 'rgba(245, 158, 11, 0.3)'}`,
              padding: '4px 10px',
              borderRadius: '12px',
              cursor: 'pointer'
            }}
            title="Configure API Gateway URL"
          >
            <div style={{
              width: '7px',
              height: '7px',
              borderRadius: '50%',
              background: isApiConnected ? '#10B981' : '#F59E0B',
              boxShadow: `0 0 8px ${isApiConnected ? '#10B981' : '#F59E0B'}`
            }} />
            <span style={{ fontSize: '10.5px', fontWeight: '800', color: isApiConnected ? '#10B981' : '#F59E0B' }}>
              {isApiConnected ? `API CONNECTED (${apiLatency}ms)` : 'CONNECTING...'}
            </span>
          </div>

          {/* Refresh & Settings */}
          <button
            onClick={async () => {
              const tplRes = await CgApi.getTemplates();
              if (tplRes && tplRes.ok && Array.isArray(tplRes.data)) {
                setTemplates(mapApiTemplates(tplRes.data));
              }
            }}
            style={{
              padding: '5px 10px',
              borderRadius: '4px',
              background: '#1A2333',
              border: '1px solid #2B3A52',
              color: '#FFFFFF',
              fontSize: '11px',
              fontWeight: '700',
              cursor: 'pointer'
            }}
          >
            REFRESH
          </button>

          <button
            onClick={() => setShowConfigModal(true)}
            style={{
              padding: '6px',
              borderRadius: '4px',
              background: '#1A2333',
              border: '1px solid #2B3A52',
              color: '#94A3B8',
              cursor: 'pointer'
            }}
            title="Gateway Settings"
          >
            <Settings size={15} />
          </button>
        </div>
      </header>

      {/* 2. MAIN WORKSPACE: TOP SWITCHER ROW + LOWER 3-COLUMN WORKSPACE */}
      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', gap: '6px', padding: '6px', overflow: 'hidden' }}>

        {/* TOP ROW: PREVIEW BUS | SWITCHER | PROGRAM BUS */}
        {(visiblePanels.preview || visiblePanels.switcher || visiblePanels.program) && (
          <div style={{
            display: 'grid',
            gridTemplateColumns: `${visiblePanels.preview ? '1fr' : '0fr'} ${visiblePanels.switcher ? '230px' : '0fr'} ${visiblePanels.program ? '1fr' : '0fr'}`,
            gap: '8px',
            height: '350px',
            minHeight: '280px'
          }}>

            {/* PREVIEW BUS PANEL */}
            {visiblePanels.preview && (
              <div style={{
                background: '#090D14',
                border: '1px solid #6E45A1',
                borderRadius: '6px',
                display: 'flex',
                flexDirection: 'column',
                overflow: 'hidden',
                padding: '6px'
              }}>
                <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '6px' }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                    <div style={{ width: '8px', height: '8px', borderRadius: '50%', background: '#C89BED', boxShadow: '0 0 8px #C89BED' }} />
                    <span style={{ fontSize: '13px', fontWeight: '800', color: '#C89BED', letterSpacing: '0.04em' }}>
                      PREVIEW BUS
                    </span>
                  </div>
                  <span style={{ fontSize: '11px', color: '#B7C0C8', fontWeight: '600', maxWidth: '240px', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                    {previewGraphic ? previewGraphic.name : 'NO TEMPLATE CUED'}
                  </span>
                </div>

                {/* 16:9 Preview Viewport rendering exact CgCompositor frame */}
                <div style={{ flex: 1, position: 'relative', borderRadius: '4px', overflow: 'hidden', border: '1px solid #31204A', display: 'flex', alignItems: 'center', justifyContent: 'center', background: '#05070A' }}>
                  <GraphicMonitor
                    graphic={previewGraphic}
                    busName="PREVIEW"
                    isLive={false}
                    showSafeGuides={showSafeGuides}
                    onToggleSafeGuides={() => setShowSafeGuides(!showSafeGuides)}
                    channelStatus={playoutChannelStatus}
                    activeVideoUrl={previewPlayoutVideo || activePlayoutVideo}
                  />
                </div>

                {/* Bottom Bus Buttons */}
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: '6px', marginTop: '6px' }}>
                  <button
                    onClick={() => handlePlayPreview()}
                    style={{
                      padding: '8px',
                      borderRadius: '4px',
                      background: '#2B1740',
                      border: '1px solid #7445A8',
                      color: '#FFFFFF',
                      fontSize: '11px',
                      fontWeight: '800',
                      letterSpacing: '0.03em',
                      cursor: 'pointer'
                    }}
                  >
                    PLAY PREVIEW
                  </button>
                  <button
                    onClick={handleStopPreview}
                    style={{
                      padding: '8px',
                      borderRadius: '4px',
                      background: '#352618',
                      border: '1px solid #9E682B',
                      color: '#FBBF24',
                      fontSize: '11px',
                      fontWeight: '700',
                      cursor: 'pointer'
                    }}
                    title="Play out-animation and stop"
                  >
                    STOP
                  </button>
                  <button
                    onClick={handleClearPreview}
                    style={{
                      padding: '8px',
                      borderRadius: '4px',
                      background: '#1F2937',
                      border: '1px solid #374151',
                      color: '#9CA3AF',
                      fontSize: '11px',
                      fontWeight: '700',
                      cursor: 'pointer'
                    }}
                  >
                    CLEAR
                  </button>
                </div>
              </div>
            )}

            {/* SWITCHER PANEL */}
            {visiblePanels.switcher && (
              <div style={{
                background: '#0D121B',
                border: '1px solid #1E293B',
                borderRadius: '6px',
                padding: '12px 10px',
                display: 'flex',
                flexDirection: 'column',
                justifyContent: 'center',
                gap: '8px'
              }}>
                <div style={{ textAlign: 'center', fontSize: '13px', fontWeight: '800', color: '#DCC7F0', letterSpacing: '0.08em', marginBottom: '2px' }}>
                  SWITCHER
                </div>

                <button
                  onClick={() => executeTake(takeTransition === 'NONE' ? 'FADE' : takeTransition)}
                  style={{
                    padding: '10px',
                    borderRadius: '5px',
                    background: '#28394D',
                    border: '1px solid #4D85B9',
                    color: '#FFFFFF',
                    fontSize: '11.5px',
                    fontWeight: '800',
                    cursor: 'pointer'
                  }}
                  title="TAKE using selected transition; Fade if None"
                >
                  AUTO
                </button>

                <button
                  onClick={() => executeTake('CUT')}
                  style={{
                    padding: '10px',
                    borderRadius: '5px',
                    background: '#31373D',
                    border: '1px solid #77828C',
                    color: '#FFFFFF',
                    fontSize: '11.5px',
                    fontWeight: '800',
                    cursor: 'pointer'
                  }}
                  title="Immediate TAKE with no transition"
                >
                  CUT
                </button>

                <button
                  onClick={() => executeTake(takeTransition)}
                  style={{
                    padding: '12px',
                    borderRadius: '5px',
                    background: '#522B70',
                    border: '1px solid #B06FE2',
                    color: '#FFFFFF',
                    fontSize: '13px',
                    fontWeight: '800',
                    letterSpacing: '0.06em',
                    boxShadow: '0 0 14px rgba(176, 111, 226, 0.35)',
                    cursor: 'pointer'
                  }}
                  title="TAKE PREVIEW TO PROGRAM using selected optional transition"
                >
                  TAKE
                </button>

                <div style={{ marginTop: '4px' }}>
                  <label style={{ fontSize: '9px', fontWeight: '800', color: '#94A3B8', display: 'block', marginBottom: '3px' }}>
                    TAKE TRANSITION:
                  </label>
                  <select
                    value={takeTransition}
                    onChange={e => setTakeTransition(e.target.value)}
                    style={{
                      width: '100%',
                      padding: '5px 8px',
                      borderRadius: '4px',
                      background: '#161F2E',
                      border: '1px solid #2A364F',
                      color: '#FFFFFF',
                      fontSize: '11px',
                      fontWeight: '700',
                      outline: 'none',
                      cursor: 'pointer'
                    }}
                  >
                    <option value="CUT">CUT (Immediate)</option>
                    <option value="FADE">FADE (Crossfade)</option>
                    <option value="WIPE LEFT">WIPE LEFT</option>
                    <option value="WIPE RIGHT">WIPE RIGHT</option>
                    <option value="PUSH UP">PUSH UP</option>
                    <option value="ZOOM">ZOOM</option>
                  </select>
                </div>

                <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: '6px' }}>
                  <span style={{ fontSize: '9px', color: '#94A3B8', fontWeight: '700' }}>DURATION SEC:</span>
                  <input
                    type="number"
                    step="0.05"
                    min="0"
                    max="5"
                    value={takeDurationSeconds}
                    onChange={e => setTakeDurationSeconds(parseFloat(e.target.value) || 0.45)}
                    style={{
                      width: '68px',
                      padding: '4px 6px',
                      borderRadius: '4px',
                      background: '#161F2E',
                      border: '1px solid #2A364F',
                      color: '#FFFFFF',
                      fontSize: '11px',
                      textAlign: 'right',
                      outline: 'none'
                    }}
                  />
                </div>

                <div style={{ fontSize: '9px', color: '#64748B', lineHeight: '1.3', textAlign: 'center', marginTop: '2px' }}>
                  None keeps current direct/additive TAKE.
                </div>
              </div>
            )}

            {/* PROGRAM BUS PANEL */}
            {visiblePanels.program && (
              <div style={{
                background: '#090D14',
                border: '1px solid #A7434E',
                borderRadius: '6px',
                display: 'flex',
                flexDirection: 'column',
                overflow: 'hidden',
                padding: '6px'
              }}>
                <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '6px' }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                    <div className="tally-live" style={{ width: '8px', height: '8px', borderRadius: '50%', background: '#EF4444' }} />
                    <span style={{ fontSize: '13px', fontWeight: '800', color: '#FF7777', letterSpacing: '0.04em' }}>
                      PROGRAM BUS
                    </span>
                  </div>
                  <span style={{ fontSize: '11px', color: '#E0B5B8', fontWeight: '600', maxWidth: '240px', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                    {programGraphic ? programGraphic.name : 'PROGRAM CLEAR'}
                  </span>
                </div>

                {/* 16:9 Program Viewport rendering exact on-air CgCompositor frame */}
                <div style={{ flex: 1, position: 'relative', borderRadius: '4px', overflow: 'hidden', border: '1px solid #4A282D', display: 'flex', alignItems: 'center', justifyContent: 'center', background: '#05070A' }}>
                  <GraphicMonitor
                    graphic={programGraphic}
                    busName="PROGRAM"
                    isLive={true}
                    showSafeGuides={showSafeGuides}
                    onToggleSafeGuides={() => setShowSafeGuides(!showSafeGuides)}
                    transition={takeTransition}
                    transitionProgress={transitionProgress}
                    channelStatus={playoutChannelStatus}
                    activeVideoUrl={activePlayoutVideo}
                  />
                </div>

                {/* Bottom Bus Buttons */}
                <div style={{ display: 'grid', gridTemplateColumns: '1.3fr 1fr 1fr 1fr', gap: '6px', marginTop: '6px' }}>
                  <button
                    onClick={() => handlePlayProgram()}
                    style={{
                      padding: '8px 4px',
                      borderRadius: '4px',
                      background: '#3A1417',
                      border: '1px solid #A7434E',
                      color: '#FFFFFF',
                      fontSize: '11px',
                      fontWeight: '800',
                      letterSpacing: '0.02em',
                      cursor: 'pointer'
                    }}
                  >
                    PLAY PROGRAM
                  </button>
                  <button
                    onClick={handleStopProgram}
                    style={{
                      padding: '8px 4px',
                      borderRadius: '4px',
                      background: '#3D2111',
                      border: '1px solid #C76E2A',
                      color: '#FB923C',
                      fontSize: '11px',
                      fontWeight: '700',
                      cursor: 'pointer'
                    }}
                    title="Animate OUT gracefully and stop"
                  >
                    STOP
                  </button>
                  <button
                    onClick={handleUpdateLiveText}
                    style={{
                      padding: '8px 4px',
                      borderRadius: '4px',
                      background: '#1F2937',
                      border: '1px solid #374151',
                      color: '#60A5FA',
                      fontSize: '11px',
                      fontWeight: '700',
                      cursor: 'pointer'
                    }}
                    title="Hot-update text live on air"
                  >
                    UPDATE
                  </button>
                  <button
                    onClick={handleClearProgram}
                    style={{
                      padding: '8px 4px',
                      borderRadius: '4px',
                      background: '#1F2937',
                      border: '1px solid #374151',
                      color: '#9CA3AF',
                      fontSize: '11px',
                      fontWeight: '700',
                      cursor: 'pointer'
                    }}
                  >
                    CLEAR
                  </button>
                </div>
              </div>
            )}
          </div>
        )}

        {/* LOWER ROW: 3-COLUMN WORKSPACE (CG CATALOG | LAYERS & QUICK EDITS | TARGETS & OUTPUT) */}
        <div style={{
          flex: 1,
          display: 'grid',
          gridTemplateColumns: `${visiblePanels.catalog ? '1.3fr' : '0fr'} ${visiblePanels.layers ? '1.4fr' : '0fr'} ${visiblePanels.targets ? '1.1fr' : '0fr'}`,
          gap: '8px',
          minHeight: 0,
          overflow: 'hidden'
        }}>

          {/* COLUMN 1: CG CATALOG */}
          {visiblePanels.catalog && (
            <div style={{
              background: '#0B0F17',
              border: '1px solid #1E293B',
              borderRadius: '6px',
              padding: '8px',
              display: 'flex',
              flexDirection: 'column',
              minHeight: 0
            }}>
              {/* Catalog Header with View Switcher */}
              <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '6px' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                  <span style={{ fontSize: '12px', fontWeight: '800', color: '#FFFFFF' }}>CG CATALOG</span>
                  <span style={{ fontSize: '9.5px', color: '#64748B' }}>· {templates.length} templates in DB</span>
                </div>
                <div style={{ display: 'flex', gap: '3px', background: '#161F2E', padding: '2px', borderRadius: '4px' }}>
                  <button
                    onClick={() => setCatalogViewMode('LIST')}
                    style={{
                      padding: '3px 8px',
                      borderRadius: '3px',
                      fontSize: '10px',
                      fontWeight: '700',
                      background: catalogViewMode === 'LIST' ? '#3B82F6' : 'transparent',
                      color: catalogViewMode === 'LIST' ? '#FFFFFF' : '#94A3B8',
                      cursor: 'pointer'
                    }}
                  >
                    LIST
                  </button>
                  <button
                    onClick={() => setCatalogViewMode('GRID')}
                    style={{
                      padding: '3px 8px',
                      borderRadius: '3px',
                      fontSize: '10px',
                      fontWeight: '700',
                      background: catalogViewMode === 'GRID' ? '#3B82F6' : 'transparent',
                      color: catalogViewMode === 'GRID' ? '#FFFFFF' : '#94A3B8',
                      cursor: 'pointer'
                    }}
                  >
                    GRID
                  </button>
                </div>
              </div>

              {/* Search Bar */}
              <div style={{ position: 'relative', marginBottom: '6px' }}>
                <Search size={12} style={{ position: 'absolute', left: '8px', top: '50%', transform: 'translateY(-50%)', color: '#64748B' }} />
                <input
                  type="text"
                  placeholder={`Search ${templates.length} CG templates...`}
                  value={searchQuery}
                  onChange={e => setSearchQuery(e.target.value)}
                  style={{
                    width: '100%',
                    padding: '5px 24px 5px 26px',
                    background: '#06090E',
                    border: '1px solid #1E293B',
                    borderRadius: '4px',
                    color: '#FFFFFF',
                    fontSize: '11px',
                    outline: 'none'
                  }}
                />
                {searchQuery && (
                  <button
                    onClick={() => setSearchQuery('')}
                    style={{ position: 'absolute', right: '6px', top: '50%', transform: 'translateY(-50%)', background: 'none', color: '#64748B', cursor: 'pointer', fontSize: '10px' }}
                  >
                    ✕
                  </button>
                )}
              </div>

              {/* Dynamic Category Chips */}
              <div style={{ display: 'flex', gap: '3px', overflowX: 'auto', marginBottom: '8px', paddingBottom: '2px' }}>
                {availableCategories.map(cat => (
                  <button
                    key={cat}
                    onClick={() => setActiveCategory(cat)}
                    style={{
                      padding: '3px 7px',
                      borderRadius: '3px',
                      fontSize: '9.5px',
                      fontWeight: '700',
                      background: activeCategory === cat ? '#6E45A1' : '#161F2E',
                      color: activeCategory === cat ? '#FFFFFF' : '#94A3B8',
                      whiteSpace: 'nowrap',
                      border: 'none',
                      cursor: 'pointer'
                    }}
                  >
                    {cat}
                  </button>
                ))}
              </div>

              {/* Templates View (Grid or List) with Real CgThumbnailRenderer Thumbnails */}
              <div style={{ flex: 1, overflowY: 'auto', minHeight: 0 }}>
                {filteredTemplates.length === 0 ? (
                  <div style={{ padding: '24px 12px', textAlign: 'center', color: '#64748B' }}>
                    <div style={{ fontSize: '11.5px', fontWeight: '700', color: '#94A3B8', marginBottom: '6px' }}>NO TEMPLATES FOUND</div>
                    <button
                      onClick={handleSeedDemo}
                      style={{ padding: '6px 14px', borderRadius: '4px', background: '#6E45A1', color: '#FFFFFF', fontSize: '11px', fontWeight: '700', cursor: 'pointer' }}
                    >
                      LOAD CANONICAL DEMO CG
                    </button>
                  </div>
                ) : catalogViewMode === 'GRID' ? (
                  <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(170px, 1fr))', gap: '8px' }}>
                    {filteredTemplates.map(tpl => {
                      const isSelected = tpl.id === selectedTemplateId;
                      const isAir = programGraphic?.name === tpl.name;
                      const isCued = previewGraphic?.name === tpl.name;
                      const thumbUrl = tpl.thumbnailUrl
                        ? `${apiBaseUrl}${tpl.thumbnailUrl}&apiKey=${encodeURIComponent(getApiKey())}`
                        : `${apiBaseUrl}/api/v1/cg/thumbnail?name=${encodeURIComponent(tpl.name)}&apiKey=${encodeURIComponent(getApiKey())}`;

                      return (
                        <div
                          key={tpl.id}
                          onClick={() => setSelectedTemplateId(tpl.id)}
                          style={{
                            background: isSelected ? '#161F2E' : '#0D131C',
                            border: isAir ? '1px solid #DC2626' : isCued ? '1px solid #10B981' : isSelected ? '1px solid #6E45A1' : '1px solid #34404C',
                            borderRadius: '3px',
                            padding: '6px',
                            cursor: 'pointer',
                            display: 'flex',
                            flexDirection: 'column',
                            gap: '4px'
                          }}
                        >
                          {/* Real 16:9 Thumbnail Image from CgThumbnailRenderer matching desktop */}
                          <div style={{
                            width: '100%',
                            aspectRatio: '16/9',
                            background: '#000000',
                            borderRadius: '2px',
                            position: 'relative',
                            overflow: 'hidden'
                          }}>
                            <img
                              src={thumbUrl}
                              alt={tpl.name}
                              loading="lazy"
                              style={{
                                width: '100%',
                                height: '100%',
                                objectFit: 'cover',
                                display: 'block'
                              }}
                              onError={(e) => {
                                e.target.style.display = 'none';
                              }}
                            />

                            {isAir && (
                              <span style={{ position: 'absolute', top: '3px', right: '3px', background: '#DC2626', color: '#FFFFFF', fontSize: '7.5px', fontWeight: '800', padding: '1px 5px', borderRadius: '2px' }}>
                                AIR
                              </span>
                            )}
                            {isCued && (
                              <span style={{ position: 'absolute', top: '3px', right: '3px', background: '#10B981', color: '#FFFFFF', fontSize: '7.5px', fontWeight: '800', padding: '1px 5px', borderRadius: '2px' }}>
                                CUED
                              </span>
                            )}
                          </div>

                          <div style={{ fontSize: '11px', fontWeight: '600', color: '#FFFFFF', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', marginTop: '2px' }}>
                            {tpl.name}
                          </div>
                          <div style={{ fontSize: '8.5px', color: '#94A3B8' }}>
                            {tpl.category} · {tpl.layers?.length || 0} layers
                          </div>

                          {/* Quick Actions */}
                          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '4px', marginTop: '2px' }}>
                            <button
                              onClick={(e) => { e.stopPropagation(); setSelectedTemplateId(tpl.id); handlePlayPreview(tpl); }}
                              style={{ padding: '3px', borderRadius: '2px', background: '#2B1740', border: '1px solid #7445A8', color: '#C89BED', fontSize: '9px', fontWeight: '800', cursor: 'pointer' }}
                            >
                              CUE
                            </button>
                            <button
                              onClick={(e) => { e.stopPropagation(); setSelectedTemplateId(tpl.id); handlePlayProgram(tpl); }}
                              style={{ padding: '3px', borderRadius: '2px', background: '#3A1417', border: '1px solid #A7434E', color: '#FF7777', fontSize: '9px', fontWeight: '800', cursor: 'pointer' }}
                            >
                              AIR
                            </button>
                          </div>
                        </div>
                      );
                    })}
                  </div>
                ) : (
                  /* LIST MODE */
                  <div style={{ display: 'flex', flexDirection: 'column', gap: '3px' }}>
                    {filteredTemplates.map(tpl => {
                      const isSelected = tpl.id === selectedTemplateId;
                      const thumbUrl = tpl.thumbnailUrl
                        ? `${apiBaseUrl}${tpl.thumbnailUrl}&apiKey=${encodeURIComponent(getApiKey())}`
                        : `${apiBaseUrl}/api/v1/cg/thumbnail?name=${encodeURIComponent(tpl.name)}&apiKey=${encodeURIComponent(getApiKey())}`;

                      return (
                        <div
                          key={tpl.id}
                          onClick={() => setSelectedTemplateId(tpl.id)}
                          style={{
                            display: 'flex',
                            alignItems: 'center',
                            justifyContent: 'space-between',
                            padding: '4px 6px',
                            background: isSelected ? '#161F2E' : '#0D131C',
                            border: isSelected ? '1px solid #6E45A1' : '1px solid #1E293B',
                            borderRadius: '4px',
                            cursor: 'pointer'
                          }}
                        >
                          <div style={{ display: 'flex', alignItems: 'center', gap: '8px', minWidth: 0 }}>
                            <div style={{ width: '48px', height: '27px', background: '#000', borderRadius: '2px', overflow: 'hidden', flexShrink: 0 }}>
                              <img src={thumbUrl} alt="" style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
                            </div>
                            <span style={{ fontSize: '11px', fontWeight: '700', color: '#FFFFFF', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                              {tpl.name}
                            </span>
                          </div>
                          <div style={{ display: 'flex', gap: '4px' }}>
                            <button
                              onClick={(e) => { e.stopPropagation(); handlePlayPreview(tpl); }}
                              style={{ padding: '2px 6px', borderRadius: '3px', background: '#2B1740', color: '#C89BED', fontSize: '9px', fontWeight: '700' }}
                            >
                              CUE
                            </button>
                            <button
                              onClick={(e) => { e.stopPropagation(); handlePlayProgram(tpl); }}
                              style={{ padding: '2px 6px', borderRadius: '3px', background: '#3A1417', color: '#FF7777', fontSize: '9px', fontWeight: '700' }}
                            >
                              AIR
                            </button>
                          </div>
                        </div>
                      );
                    })}
                  </div>
                )}
              </div>

              {/* Bottom Catalog Action Buttons */}
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: '6px', marginTop: '6px', paddingTop: '6px', borderTop: '1px solid #1E293B' }}>
                <input
                  type="file"
                  ref={fileInputRef}
                  accept=".kcg,.kashtrixcg,.json"
                  style={{ display: 'none' }}
                  onChange={handleImportFile}
                />
                <button
                  onClick={() => fileInputRef.current?.click()}
                  style={{ padding: '6px 4px', borderRadius: '4px', background: '#161F2E', border: '1px solid #2A364F', color: '#FFFFFF', fontSize: '10px', fontWeight: '700', cursor: 'pointer' }}
                  title="Import external CG template or project file (.json, .cgproj, .kcg)"
                >
                  IMPORT CG
                </button>
                <button
                  onClick={async () => {
                    const tplRes = await CgApi.getTemplates();
                    if (tplRes && tplRes.ok && Array.isArray(tplRes.data)) {
                      setTemplates(mapApiTemplates(tplRes.data));
                    }
                  }}
                  style={{ padding: '6px 4px', borderRadius: '4px', background: '#161F2E', border: '1px solid #2A364F', color: '#FFFFFF', fontSize: '10px', fontWeight: '700', cursor: 'pointer' }}
                  title="Reload all CG templates from database and factory defaults"
                >
                  RELOAD
                </button>
                <button
                  onClick={handleSeedDemo}
                  style={{ padding: '6px 4px', borderRadius: '4px', background: '#2B1740', border: '1px solid #7445A8', color: '#C89BED', fontSize: '10px', fontWeight: '800', cursor: 'pointer' }}
                  title="Load built-in canonical broadcast demo templates"
                >
                  CANONICAL DEMO
                </button>
              </div>
            </div>
          )}

          {/* COLUMN 2: LAYERS & CONTROLS (TABS MATCHING DESKTOP) */}
          {visiblePanels.layers && (
            <div style={{
              background: '#0B0F17',
              border: '1px solid #1E293B',
              borderRadius: '6px',
              padding: '8px',
              display: 'flex',
              flexDirection: 'column',
              minHeight: 0
            }}>
              {/* Tab Selector */}
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(5, 1fr)', gap: '4px', marginBottom: '8px' }}>
                {[
                  { key: 'LAYERS', label: `LAYERS (${selectedTemplate?.layers?.length || 0})` },
                  { key: 'OVERLAYS', label: 'OVERLAYS' },
                  { key: 'ONAIR', label: `ON AIR (${onAirItems.length})` },
                  { key: 'QUICKEDIT', label: 'QUICK EDIT' },
                  { key: 'SCHEDULER', label: 'SCHEDULER' }
                ].map(t => (
                  <button
                    key={t.key}
                    onClick={() => setCenterTab(t.key)}
                    style={{
                      padding: '5px 2px',
                      borderRadius: '4px',
                      fontSize: '9.5px',
                      fontWeight: '800',
                      letterSpacing: '0.02em',
                      background: centerTab === t.key ? '#6E45A1' : '#161F2E',
                      color: centerTab === t.key ? '#FFFFFF' : '#94A3B8',
                      border: 'none',
                      cursor: 'pointer'
                    }}
                  >
                    {t.label}
                  </button>
                ))}
              </div>

              {/* TAB 1: LAYERS · SELECTED CG */}
              {centerTab === 'LAYERS' && (
                <div style={{ flex: 1, display: 'flex', flexDirection: 'column', gap: '6px', minHeight: 0 }}>
                  <div style={{ flex: 1, overflowY: 'auto', display: 'flex', flexDirection: 'column', gap: '4px' }}>
                    {(!selectedTemplate?.layers || selectedTemplate.layers.length === 0) ? (
                      <div style={{ padding: '24px 12px', textAlign: 'center', color: '#64748B', fontSize: '11px' }}>
                        No layers in selected composition
                      </div>
                    ) : (
                      selectedTemplate.layers.map((lay, idx) => {
                        const isText = lay.type === 'Text' || lay.type === 'Ticker';
                        const isSelected = selectedLayerIndex === idx;
                        const endSec = (lay.startSeconds || 0) + (lay.durationSeconds || selectedTemplate.autoOut || 8);

                        return (
                          <div
                            key={lay.id || idx}
                            onClick={() => {
                              setSelectedLayerIndex(idx);
                              if (lay.text) {
                                setEditLine1(lay.text);
                              }
                            }}
                            style={{
                              display: 'flex',
                              alignItems: 'center',
                              justifyContent: 'space-between',
                              padding: '6px 8px',
                              borderRadius: '4px',
                              background: isSelected ? '#161F2E' : '#0D131C',
                              border: isSelected ? '1px solid #38BDF8' : '1px solid #1E293B',
                              cursor: 'pointer'
                            }}
                          >
                            <div style={{ display: 'flex', alignItems: 'center', gap: '8px', minWidth: 0 }}>
                              <span style={{
                                padding: '1px 5px',
                                borderRadius: '3px',
                                fontSize: '8.5px',
                                fontWeight: '800',
                                background: isText ? 'rgba(99, 102, 241, 0.2)' : 'rgba(56, 189, 248, 0.2)',
                                color: isText ? '#818CF8' : '#38BDF8'
                              }}>
                                {lay.type}
                              </span>
                              <div style={{ minWidth: 0 }}>
                                <div style={{ fontSize: '11px', fontWeight: '700', color: '#FFFFFF', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                                  {lay.name}
                                </div>
                                {lay.text && (
                                  <div style={{ fontSize: '9.5px', color: '#94A3B8', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                                    {lay.text}
                                  </div>
                                )}
                              </div>
                            </div>

                            <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                              <span style={{ fontSize: '9px', fontFamily: 'monospace', color: '#7EE787', background: '#16222E', padding: '1px 4px', borderRadius: '2px' }}>
                                {(lay.startSeconds || 0).toFixed(2)}s
                              </span>
                              <span style={{ fontSize: '9px', color: '#64748B' }}>➔</span>
                              <span style={{ fontSize: '9px', fontFamily: 'monospace', color: '#B98CF1', background: '#261A3A', padding: '1px 4px', borderRadius: '2px' }}>
                                {endSec.toFixed(2)}s
                              </span>
                            </div>
                          </div>
                        );
                      })
                    )}
                  </div>

                  {/* Layer Inspection Box */}
                  <div style={{ background: '#070A0F', border: '1px solid #1E293B', borderRadius: '4px', padding: '6px 8px' }}>
                    <div style={{ fontSize: '10px', fontWeight: '700', color: '#C89BED' }}>
                      {selectedTemplate?.layers?.[selectedLayerIndex]?.name || 'No layer selected'}
                    </div>
                    <div style={{ fontSize: '9.5px', color: '#94A3B8', marginTop: '2px' }}>
                      {selectedTemplate?.layers?.[selectedLayerIndex]?.text || 'PLAY routes the complete selected CG project as one composition; this list is for layer inspection.'}
                    </div>
                  </div>
                </div>
              )}

              {/* TAB 2: OVERLAYS */}
              {centerTab === 'OVERLAYS' && (
                <div style={{ flex: 1, overflowY: 'auto', display: 'flex', flexDirection: 'column', gap: '8px' }}>
                  <div style={{ fontSize: '12px', fontWeight: '800', color: '#DCC7F0' }}>MULTI-OVERLAY MANAGER</div>
                  <div style={{ fontSize: '9.5px', color: '#94A3B8' }}>Select, add, and clear dynamic graphic overlays.</div>

                  <div style={{ background: '#1C1428', border: '1px solid #7445A8', borderRadius: '4px', padding: '8px' }}>
                    <div style={{ fontSize: '11px', fontWeight: '800', color: '#DCC7F0' }}>
                      ACTIVE CHANNEL: OVERLAY {selectedOverlayChannel}
                    </div>
                  </div>

                  <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: '6px' }}>
                    {[0, 1, 2, 3, 4, 5].map(ch => (
                      <button
                        key={ch}
                        onClick={() => setSelectedOverlayChannel(ch)}
                        style={{
                          padding: '8px',
                          borderRadius: '4px',
                          background: selectedOverlayChannel === ch ? '#522B70' : '#161F2E',
                          border: selectedOverlayChannel === ch ? '1px solid #B06FE2' : '1px solid #2A364F',
                          color: '#FFFFFF',
                          fontSize: '10.5px',
                          fontWeight: '800',
                          cursor: 'pointer'
                        }}
                      >
                        OVERLAY {ch}
                      </button>
                    ))}
                  </div>

                  <button
                    onClick={() => {
                      const next = activeOverlayLayers.length;
                      setActiveOverlayLayers(prev => [...prev, next]);
                      setSelectedOverlayChannel(next);
                    }}
                    style={{ padding: '8px', borderRadius: '4px', background: '#3E2359', border: '1px solid #8055B3', color: '#FFFFFF', fontSize: '10.5px', fontWeight: '700', cursor: 'pointer' }}
                  >
                    + ADD OVERLAY CHANNEL
                  </button>

                  <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '6px', marginTop: 'auto' }}>
                    <button
                      onClick={() => CgApi.clear(selectedTarget, selectedOverlayChannel, 'PROGRAM')}
                      style={{ padding: '7px', borderRadius: '4px', background: '#251B31', border: '1px solid #4D3265', color: '#FFFFFF', fontSize: '10px', fontWeight: '700', cursor: 'pointer' }}
                    >
                      CLEAR CURRENT
                    </button>
                    <button
                      onClick={() => CgApi.clear(selectedTarget, -1, 'PROGRAM')}
                      style={{ padding: '7px', borderRadius: '4px', background: '#5B1E22', border: '1px solid #C0393B', color: '#FFFFFF', fontSize: '10px', fontWeight: '800', cursor: 'pointer' }}
                    >
                      CLEAR ALL OVERLAYS
                    </button>
                  </div>
                </div>
              )}

              {/* TAB 3: ON AIR · PROGRAM */}
              {centerTab === 'ONAIR' && (
                <div style={{ flex: 1, display: 'flex', flexDirection: 'column', gap: '6px', minHeight: 0 }}>
                  <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                    <span style={{ fontSize: '11px', fontWeight: '800', color: '#FF7777' }}>ON AIR · ACTIVE LAYERS</span>
                    <span style={{ fontSize: '9.5px', color: '#94A3B8' }}>{onAirItems.length} active layers</span>
                  </div>

                  <div style={{ flex: 1, overflowY: 'auto', display: 'flex', flexDirection: 'column', gap: '4px' }}>
                    {onAirItems.length === 0 ? (
                      <div style={{ padding: '24px 12px', textAlign: 'center', color: '#64748B', fontSize: '11px' }}>
                        PROGRAM BUS IS CLEAR (No graphics active on air)
                      </div>
                    ) : (
                      onAirItems.map((item, idx) => {
                        const layerText = item.source === 'LOGO' ? 'L0' : item.layer < 0 ? 'MAIN' : `L${item.layer}`;
                        return (
                          <div
                            key={idx}
                            style={{
                              display: 'flex',
                              alignItems: 'center',
                              justifyContent: 'space-between',
                              padding: '6px 8px',
                              background: '#161218',
                              border: '1px solid #A7434E',
                              borderRadius: '4px'
                            }}
                          >
                            <div style={{ display: 'flex', alignItems: 'center', gap: '6px', minWidth: 0 }}>
                              <span style={{ background: '#7F1D1D', color: '#FFF', fontSize: '9px', fontWeight: '800', padding: '1px 5px', borderRadius: '2px' }}>
                                {layerText}
                              </span>
                              <div style={{ minWidth: 0 }}>
                                <div style={{ fontSize: '11px', fontWeight: '700', color: '#FFFFFF', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                                  {item.name}
                                </div>
                                <div style={{ fontSize: '9px', color: '#94A3B8' }}>
                                  {item.channelId} · {item.elapsedSeconds ? `${Math.floor(item.elapsedSeconds)}s` : 'LIVE'}
                                </div>
                              </div>
                            </div>

                            <div style={{ display: 'flex', gap: '4px' }}>
                              <button
                                onClick={() => handleStopOnAirItem(item, false)}
                                style={{ padding: '3px 8px', borderRadius: '3px', background: '#3D2111', color: '#FB923C', fontSize: '9.5px', fontWeight: '700', border: 'none' }}
                                title="Animate out gracefully"
                              >
                                STOP
                              </button>
                              <button
                                onClick={() => handleStopOnAirItem(item, true)}
                                style={{ padding: '3px 8px', borderRadius: '3px', background: '#1F2937', color: '#EF4444', fontSize: '9.5px', fontWeight: '700', border: 'none' }}
                                title="Instant cut clear"
                              >
                                CLEAR
                              </button>
                            </div>
                          </div>
                        );
                      })
                    )}
                  </div>

                  <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: '4px', paddingTop: '4px', borderTop: '1px solid #1E293B' }}>
                    <button
                      onClick={() => handleStopProgram()}
                      style={{ padding: '6px', borderRadius: '3px', background: '#3D2111', color: '#FB923C', fontSize: '9.5px', fontWeight: '800', border: 'none' }}
                    >
                      STOP ALL
                    </button>
                    <button
                      onClick={() => handleClearProgram()}
                      style={{ padding: '6px', borderRadius: '3px', background: '#5B1E22', color: '#FFFFFF', fontSize: '9.5px', fontWeight: '800', border: 'none' }}
                    >
                      CLEAR ALL
                    </button>
                    <button
                      onClick={async () => {
                        const res = await CgApi.getOnAir(selectedTarget);
                        if (res && res.ok && Array.isArray(res.data)) setOnAirItems(res.data);
                      }}
                      style={{ padding: '6px', borderRadius: '3px', background: '#161F2E', color: '#FFFFFF', fontSize: '9.5px', fontWeight: '700', border: 'none' }}
                    >
                      REFRESH
                    </button>
                  </div>
                </div>
              )}

              {/* TAB 4: QUICK EDIT & PROPERTIES */}
              {centerTab === 'QUICKEDIT' && (
                <div style={{ flex: 1, overflowY: 'auto', display: 'flex', flexDirection: 'column', gap: '8px' }}>
                  <div style={{ fontSize: '11px', fontWeight: '800', color: '#DCC7F0' }}>
                    QUICK EDIT & PROPERTIES · {selectedTemplate ? selectedTemplate.name : 'No Selection'}
                  </div>

                  {/* Kicker / Badge */}
                  <div>
                    <label style={{ fontSize: '9px', fontWeight: '700', color: '#94A3B8', display: 'block', marginBottom: '2px' }}>
                      BADGE / KICKER TEXT:
                    </label>
                    <input
                      type="text"
                      value={editBadge}
                      onChange={e => setEditBadge(e.target.value)}
                      style={{ width: '100%', padding: '4px 8px', background: '#070A0F', border: '1px solid #1E293B', borderRadius: '3px', color: '#FFF', fontSize: '11px' }}
                    />
                  </div>

                  {/* Headline (Line 1) */}
                  <div>
                    <label style={{ fontSize: '9px', fontWeight: '700', color: '#94A3B8', display: 'block', marginBottom: '2px' }}>
                      PRIMARY HEADLINE (LINE 1):
                    </label>
                    <input
                      type="text"
                      value={editLine1}
                      onChange={e => setEditLine1(e.target.value)}
                      style={{ width: '100%', padding: '4px 8px', background: '#070A0F', border: '1px solid #1E293B', borderRadius: '3px', color: '#FFF', fontSize: '11px' }}
                    />
                  </div>

                  {/* Subtitle (Line 2) */}
                  <div>
                    <label style={{ fontSize: '9px', fontWeight: '700', color: '#94A3B8', display: 'block', marginBottom: '2px' }}>
                      SECONDARY SUBTITLE (LINE 2):
                    </label>
                    <input
                      type="text"
                      value={editLine2}
                      onChange={e => setEditLine2(e.target.value)}
                      style={{ width: '100%', padding: '4px 8px', background: '#070A0F', border: '1px solid #1E293B', borderRadius: '3px', color: '#FFF', fontSize: '11px' }}
                    />
                  </div>

                  {/* Colors & Swatches */}
                  <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '6px' }}>
                    <div>
                      <label style={{ fontSize: '9px', fontWeight: '700', color: '#94A3B8', display: 'block', marginBottom: '2px' }}>
                        TEXT FILL COLOR:
                      </label>
                      <div style={{ display: 'flex', gap: '4px', alignItems: 'center' }}>
                        <input
                          type="color"
                          value={editColor.startsWith('#') && editColor.length === 7 ? editColor : '#EF4444'}
                          onChange={e => setEditColor(e.target.value)}
                          style={{ width: '24px', height: '24px', border: 'none', background: 'none', cursor: 'pointer' }}
                        />
                        <button onClick={() => setEditColor('#FFFFFF')} style={{ padding: '2px 5px', fontSize: '9px', background: '#334155', color: '#FFF', borderRadius: '2px' }}>WHT</button>
                        <button onClick={() => setEditColor('#FFD700')} style={{ padding: '2px 5px', fontSize: '9px', background: '#854D0E', color: '#FFF', borderRadius: '2px' }}>YEL</button>
                        <button onClick={() => setEditColor('#EF4444')} style={{ padding: '2px 5px', fontSize: '9px', background: '#991B1B', color: '#FFF', borderRadius: '2px' }}>RED</button>
                      </div>
                    </div>

                    <div>
                      <label style={{ fontSize: '9px', fontWeight: '700', color: '#94A3B8', display: 'block', marginBottom: '2px' }}>
                        PLATE BACKGROUND:
                      </label>
                      <div style={{ display: 'flex', gap: '4px', alignItems: 'center' }}>
                        <input
                          type="color"
                          value={editBgColor.startsWith('#') && editBgColor.length === 7 ? editBgColor : '#0F172A'}
                          onChange={e => setEditBgColor(e.target.value)}
                          style={{ width: '24px', height: '24px', border: 'none', background: 'none', cursor: 'pointer' }}
                        />
                        <button onClick={() => setEditBgColor('#0B1728')} style={{ padding: '2px 5px', fontSize: '9px', background: '#1E3A8A', color: '#FFF', borderRadius: '2px' }}>NAVY</button>
                        <button onClick={() => setEditBgColor('#4A0E13')} style={{ padding: '2px 5px', fontSize: '9px', background: '#7F1D1D', color: '#FFF', borderRadius: '2px' }}>DARK</button>
                        <button onClick={() => setEditBgColor('#9C7118')} style={{ padding: '2px 5px', fontSize: '9px', background: '#B45309', color: '#FFF', borderRadius: '2px' }}>GOLD</button>
                      </div>
                    </div>
                  </div>

                  {/* Action Buttons: Save & Preview / Save & Play Program */}
                  <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: '4px', marginTop: 'auto', paddingTop: '4px' }}>
                    <button
                      onClick={() => handlePlayPreview()}
                      style={{ padding: '7px 4px', borderRadius: '4px', background: '#2B1740', border: '1px solid #7445A8', color: '#C89BED', fontSize: '10px', fontWeight: '800' }}
                    >
                      SAVE & PREVIEW
                    </button>
                    <button
                      onClick={() => handlePlayProgram()}
                      style={{ padding: '7px 4px', borderRadius: '4px', background: '#3A1417', border: '1px solid #A7434E', color: '#FF7777', fontSize: '10px', fontWeight: '800' }}
                    >
                      SAVE & PROGRAM
                    </button>
                    <button
                      onClick={handleUpdateLiveText}
                      style={{ padding: '7px 4px', borderRadius: '4px', background: '#161F2E', border: '1px solid #2A364F', color: '#38BDF8', fontSize: '10px', fontWeight: '800' }}
                    >
                      HOT UPDATE
                    </button>
                  </div>
                </div>
              )}

              {/* TAB 5: CG SCHEDULER */}
              {centerTab === 'SCHEDULER' && (
                <div style={{ flex: 1, overflowY: 'auto', display: 'flex', flexDirection: 'column', gap: '6px' }}>
                  <div style={{ fontSize: '11px', fontWeight: '800', color: '#DCC7F0' }}>DYNAMIC CG BROADCAST SCHEDULER</div>
                  <div style={{ fontSize: '9px', color: '#94A3B8' }}>Set automated graphic play & clear triggers by time.</div>

                  {/* Add form */}
                  <div style={{ background: '#0D131C', border: '1px solid #1E293B', borderRadius: '4px', padding: '6px', display: 'flex', flexDirection: 'column', gap: '4px' }}>
                    <select
                      value={newSchedProject}
                      onChange={e => setNewSchedProject(e.target.value)}
                      style={{ width: '100%', padding: '4px', background: '#06090E', border: '1px solid #1E293B', color: '#FFF', fontSize: '10px' }}
                    >
                      <option value="">-- SELECT TEMPLATE --</option>
                      {templates.map(t => <option key={t.id} value={t.name}>{t.name}</option>)}
                    </select>

                    <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 60px', gap: '4px' }}>
                      <input
                        type="text"
                        placeholder="Start HH:mm:ss"
                        value={newSchedStart}
                        onChange={e => setNewSchedStart(e.target.value)}
                        style={{ padding: '4px', background: '#06090E', border: '1px solid #1E293B', color: '#FFF', fontSize: '10px' }}
                      />
                      <input
                        type="text"
                        placeholder="Stop HH:mm:ss"
                        value={newSchedStop}
                        onChange={e => setNewSchedStop(e.target.value)}
                        style={{ padding: '4px', background: '#06090E', border: '1px solid #1E293B', color: '#FFF', fontSize: '10px' }}
                      />
                      <input
                        type="number"
                        placeholder="L"
                        value={newSchedLayer}
                        onChange={e => setNewSchedLayer(parseInt(e.target.value) || 3)}
                        style={{ padding: '4px', background: '#06090E', border: '1px solid #1E293B', color: '#FFF', fontSize: '10px' }}
                      />
                    </div>

                    <button
                      onClick={() => {
                        if (!newSchedProject) return;
                        setSchedules(prev => [...prev, {
                          id: String(Date.now()),
                          projectName: newSchedProject,
                          startTime: newSchedStart,
                          stopTime: newSchedStop,
                          layer: newSchedLayer,
                          days: newSchedDays,
                          enabled: true,
                          status: 'STANDBY'
                        }]);
                      }}
                      style={{ padding: '5px', borderRadius: '3px', background: '#3E2359', border: '1px solid #8055B3', color: '#FFF', fontSize: '10px', fontWeight: '700' }}
                    >
                      + ADD SCHEDULE
                    </button>
                  </div>

                  {/* Scheduled Items List */}
                  <div style={{ flex: 1, overflowY: 'auto', display: 'flex', flexDirection: 'column', gap: '4px' }}>
                    {schedules.map(sch => (
                      <div key={sch.id} style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '5px 8px', background: '#161222', border: '1px solid #352646', borderRadius: '3px' }}>
                        <div>
                          <div style={{ fontSize: '10.5px', fontWeight: '700', color: '#FFF' }}>{sch.projectName}</div>
                          <div style={{ fontSize: '9px', color: '#C89BED', fontFamily: 'monospace' }}>
                            {sch.startTime} ➔ {sch.stopTime} · L{sch.layer} · {sch.days}
                          </div>
                        </div>
                        <button
                          onClick={() => setSchedules(prev => prev.filter(x => x.id !== sch.id))}
                          style={{ background: 'none', border: 'none', color: '#EF4444', cursor: 'pointer', fontSize: '11px' }}
                        >
                          ✕
                        </button>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>
          )}

          {/* COLUMN 3: TARGETS & OUTPUT */}
          {visiblePanels.targets && (
            <div style={{
              background: '#0B0F17',
              border: '1px solid #1E293B',
              borderRadius: '6px',
              padding: '8px',
              display: 'flex',
              flexDirection: 'column',
              minHeight: 0
            }}>
              {/* Tab Selector */}
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: '4px', marginBottom: '8px' }}>
                {['TARGETS', 'OUTPUT', 'STATUS'].map(tab => (
                  <button
                    key={tab}
                    onClick={() => setRightTab(tab)}
                    style={{
                      padding: '5px',
                      borderRadius: '4px',
                      fontSize: '10px',
                      fontWeight: '800',
                      background: rightTab === tab ? '#6E45A1' : '#161F2E',
                      color: rightTab === tab ? '#FFFFFF' : '#94A3B8',
                      border: 'none',
                      cursor: 'pointer'
                    }}
                  >
                    {tab}
                  </button>
                ))}
              </div>

              {/* TAB 1: TARGET CHANNELS */}
              {rightTab === 'TARGETS' && (
                <div style={{ flex: 1, display: 'flex', flexDirection: 'column', gap: '6px', minHeight: 0 }}>
                  <div style={{ fontSize: '11px', fontWeight: '800', color: '#DCC7F0' }}>BROADCAST CHANNELS & DESTINATIONS</div>
                  <div style={{ flex: 1, overflowY: 'auto', display: 'flex', flexDirection: 'column', gap: '4px' }}>
                    {cgTargets.map(tgt => {
                      const isChecked = selectedTargetsList.includes(tgt);
                      return (
                        <div
                          key={tgt}
                          onClick={() => {
                            setSelectedTargetsList(prev => prev.includes(tgt) ? prev.filter(x => x !== tgt) : [...prev, tgt]);
                            setSelectedTarget(tgt);
                          }}
                          style={{
                            display: 'flex',
                            alignItems: 'center',
                            justifyContent: 'space-between',
                            padding: '8px 10px',
                            background: isChecked ? '#161F2E' : '#0D131C',
                            border: isChecked ? '1px solid #38BDF8' : '1px solid #1E293B',
                            borderRadius: '4px',
                            cursor: 'pointer'
                          }}
                        >
                          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                            <input type="checkbox" checked={isChecked} onChange={() => {}} />
                            <div>
                              <div style={{ fontSize: '11px', fontWeight: '700', color: '#FFFFFF' }}>{tgt}</div>
                              <div style={{ fontSize: '9px', color: '#64748B' }}>Primary Playout Bus</div>
                            </div>
                          </div>
                          <span style={{ fontSize: '9.5px', fontWeight: '700', color: '#10B981' }}>ONLINE</span>
                        </div>
                      );
                    })}
                  </div>

                  <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '6px', paddingTop: '4px', borderTop: '1px solid #1E293B' }}>
                    <button
                      onClick={() => setSelectedTargetsList([...cgTargets])}
                      style={{ padding: '6px', borderRadius: '4px', background: '#161F2E', color: '#FFF', fontSize: '10px', fontWeight: '700' }}
                    >
                      SELECT ALL
                    </button>
                    <button
                      onClick={() => setSelectedTargetsList([])}
                      style={{ padding: '6px', borderRadius: '4px', background: '#161F2E', color: '#94A3B8', fontSize: '10px', fontWeight: '700' }}
                    >
                      CLEAR
                    </button>
                  </div>
                </div>
              )}

              {/* TAB 2: OUTPUT (NDI / DECKLINK / VIDEO FORMAT) */}
              {rightTab === 'OUTPUT' && (
                <div style={{ flex: 1, overflowY: 'auto', display: 'flex', flexDirection: 'column', gap: '8px' }}>
                  <div style={{ fontSize: '11px', fontWeight: '800', color: '#DCC7F0' }}>CG PROGRAM BUS OUTPUTS</div>

                  {/* NDI Broadcast Output */}
                  <div style={{ background: '#101824', border: '1px solid #254260', borderRadius: '4px', padding: '6px' }}>
                    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '4px' }}>
                      <span style={{ fontSize: '10.5px', fontWeight: '800', color: '#66D9E8' }}>NDI BROADCAST OUTPUT</span>
                      <button
                        onClick={() => setEnableNdiAlpha(!enableNdiAlpha)}
                        style={{ padding: '2px 6px', borderRadius: '3px', background: enableNdiAlpha ? '#0E7490' : '#334155', color: '#FFF', fontSize: '9px', fontWeight: '800' }}
                      >
                        {enableNdiAlpha ? 'ACTIVE' : 'OFF'}
                      </button>
                    </div>
                    <label style={{ fontSize: '9px', color: '#94A3B8', display: 'block', marginBottom: '2px' }}>STREAM NAME:</label>
                    <input
                      type="text"
                      value={ndiSourceName}
                      onChange={e => setNdiSourceName(e.target.value)}
                      style={{ width: '100%', padding: '4px 6px', background: '#070A0F', border: '1px solid #1E293B', borderRadius: '3px', color: '#FFF', fontSize: '10px' }}
                    />
                  </div>

                  {/* DeckLink Hardware Output */}
                  <div style={{ background: '#161224', border: '1px solid #35264E', borderRadius: '4px', padding: '6px' }}>
                    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '4px' }}>
                      <span style={{ fontSize: '10.5px', fontWeight: '800', color: '#DCC7F0' }}>DECKLINK HARDWARE KEY / FILL</span>
                      <button
                        onClick={() => setEnableDeckLink(!enableDeckLink)}
                        style={{ padding: '2px 6px', borderRadius: '3px', background: enableDeckLink ? '#7C3AED' : '#334155', color: '#FFF', fontSize: '9px', fontWeight: '800' }}
                      >
                        {enableDeckLink ? 'ENABLED' : 'DISABLED'}
                      </button>
                    </div>
                    <select
                      value={deckLinkCard}
                      onChange={e => setDeckLinkCard(e.target.value)}
                      style={{ width: '100%', padding: '4px', background: '#070A0F', border: '1px solid #1E293B', color: '#FFF', fontSize: '10px', marginBottom: '4px' }}
                    >
                      <option>DeckLink 8K Pro (1) · Key / Fill Pair</option>
                      <option>DeckLink Duo 2 (Subdevice 1)</option>
                      <option>DeckLink Quad 2 (Port A)</option>
                    </select>

                    <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '4px' }}>
                      <div>
                        <span style={{ fontSize: '9px', color: '#94A3B8', display: 'block' }}>KEYER MODE:</span>
                        <select
                          value={deckLinkKeyerMode}
                          onChange={e => setDeckLinkKeyerMode(e.target.value)}
                          style={{ width: '100%', padding: '3px', background: '#070A0F', border: '1px solid #1E293B', color: '#FFF', fontSize: '10px' }}
                        >
                          <option>Internal Keyer</option>
                          <option>External Keyer</option>
                        </select>
                      </div>
                      <div>
                        <span style={{ fontSize: '9px', color: '#94A3B8', display: 'block' }}>KEY LEVEL:</span>
                        <input
                          type="number"
                          value={deckLinkKeyLevel}
                          onChange={e => setDeckLinkKeyLevel(parseInt(e.target.value) || 255)}
                          style={{ width: '100%', padding: '3px', background: '#070A0F', border: '1px solid #1E293B', color: '#FFF', fontSize: '10px' }}
                        />
                      </div>
                    </div>
                  </div>

                  {/* Video Output Format Preset */}
                  <div>
                    <label style={{ fontSize: '9px', fontWeight: '700', color: '#94A3B8', display: 'block', marginBottom: '2px' }}>OUTPUT FORMAT PRESET:</label>
                    <select
                      value={outputPreset}
                      onChange={e => setOutputPreset(e.target.value)}
                      style={{ width: '100%', padding: '4px', background: '#070A0F', border: '1px solid #1E293B', color: '#FFF', fontSize: '10px' }}
                    >
                      <option value="1080p50">1080p50 · 1920x1080 @ 50fps Progressive</option>
                      <option value="1080p59.94">1080p59.94 · 1920x1080 @ 59.94fps Progressive</option>
                      <option value="1080i50">1080i50 · 1920x1080 @ 25fps Interlaced</option>
                      <option value="720p50">720p50 · 1280x720 @ 50fps Progressive</option>
                      <option value="2160p50">2160p50 UHD 4K · 3840x2160 @ 50fps</option>
                    </select>
                  </div>

                  <button
                    onClick={() => {
                      setLastRouteOperation({
                        name: 'APPLY CG FORMAT',
                        time: new Date().toLocaleTimeString(),
                        detail: `Applied output preset ${outputPreset} with ${prerollFrames} preroll frames`
                      });
                    }}
                    style={{ padding: '7px', borderRadius: '4px', background: '#3E2359', border: '1px solid #8055B3', color: '#FFF', fontSize: '10.5px', fontWeight: '800', marginTop: 'auto' }}
                  >
                    APPLY CG BUS FORMAT
                  </button>
                </div>
              )}

              {/* TAB 3: STATUS */}
              {rightTab === 'STATUS' && (
                <div style={{ flex: 1, overflowY: 'auto', display: 'flex', flexDirection: 'column', gap: '8px' }}>
                  <div style={{ fontSize: '11px', fontWeight: '800', color: '#DCC7F0' }}>LAST ROUTE & STATUS</div>

                  <div style={{ background: '#070A0F', border: '1px solid #1E293B', borderRadius: '4px', padding: '8px' }}>
                    <div style={{ fontSize: '9px', color: '#64748B', fontWeight: '700' }}>LAST OPERATION:</div>
                    <div style={{ fontSize: '13px', fontWeight: '800', color: '#C89BED', marginTop: '2px' }}>
                      {lastRouteOperation.name}
                    </div>
                    <div style={{ fontSize: '9px', color: '#38BDF8', fontFamily: 'monospace', marginTop: '2px' }}>
                      {lastRouteOperation.time}
                    </div>
                    <div style={{ fontSize: '10px', color: '#94A3B8', marginTop: '4px' }}>
                      {lastRouteOperation.detail}
                    </div>
                  </div>

                  <div style={{ fontSize: '10px', color: '#B98CF1', lineHeight: '1.4', background: '#161222', border: '1px solid #352646', borderRadius: '4px', padding: '8px' }}>
                    Program Bus publishing transparent CG key/fill on NDI ({ndiSourceName}) and DeckLink keyer.
                  </div>
                </div>
              )}
            </div>
          )}
        </div>
      </div>

      {/* 3. BOTTOM TICKER CONTROLLER BAR */}
      <div style={{
        height: '42px',
        backgroundColor: '#0A0E15',
        borderTop: '1px solid #1E293B',
        display: 'flex',
        alignItems: 'center',
        padding: '0 12px',
        gap: '12px',
        zIndex: 20
      }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
          <Radio size={14} color="#6E45A1" />
          <span style={{ fontSize: '10px', fontWeight: '800', color: '#FFFFFF' }}>TICKER CRAWL:</span>
        </div>

        <div style={{ display: 'flex', gap: '2px' }}>
          {[
            { label: 'SLOW', val: 40 },
            { label: 'MED', val: 28 },
            { label: 'FAST', val: 18 }
          ].map(s => (
            <button
              key={s.label}
              onClick={() => setTickerSpeed(s.val)}
              style={{
                padding: '2px 6px',
                borderRadius: '3px',
                fontSize: '9px',
                fontWeight: '700',
                background: tickerSpeed === s.val ? '#6E45A1' : '#161F2E',
                color: tickerSpeed === s.val ? '#FFFFFF' : '#94A3B8'
              }}
            >
              {s.label}
            </button>
          ))}
        </div>

        <button
          onClick={() => setIsTickerEmergency(!isTickerEmergency)}
          style={{
            padding: '3px 8px',
            borderRadius: '3px',
            fontSize: '9.5px',
            fontWeight: '800',
            background: isTickerEmergency ? '#DC2626' : '#161F2E',
            color: isTickerEmergency ? '#FFFFFF' : '#EF4444',
            border: '1px solid #EF4444'
          }}
        >
          {isTickerEmergency ? '⚡ RED FLASH ACTIVE' : '⚡ RED FLASH'}
        </button>

        <form onSubmit={handleAddTicker} style={{ flex: 1, display: 'flex', gap: '6px' }}>
          <input
            type="text"
            placeholder="Type headline to insert into live news crawl wire..."
            value={newTickerText}
            onChange={e => setNewTickerText(e.target.value)}
            style={{
              flex: 1,
              background: '#06090E',
              border: '1px solid #1E293B',
              borderRadius: '3px',
              padding: '4px 8px',
              color: '#FFFFFF',
              fontSize: '10.5px',
              outline: 'none'
            }}
          />
          <button
            type="submit"
            style={{
              padding: '4px 10px',
              borderRadius: '3px',
              background: '#6E45A1',
              color: '#FFFFFF',
              fontSize: '10px',
              fontWeight: '700'
            }}
          >
            INSERT TO TICKER
          </button>
        </form>

        <div style={{ fontSize: '9.5px', color: '#64748B', whiteSpace: 'nowrap' }}>
          INDEPENDENT PREVIEW / PROGRAM · REST/WS PORT 9080
        </div>
      </div>

      {/* 4. SETTINGS & GATEWAY MODAL */}
      {showConfigModal && (
        <div style={{
          position: 'fixed',
          inset: 0,
          background: 'rgba(0,0,0,0.75)',
          backdropFilter: 'blur(4px)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          zIndex: 100
        }}>
          <div style={{
            background: '#0D131C',
            border: '1px solid #2B384E',
            borderRadius: '8px',
            width: '420px',
            padding: '18px',
            boxShadow: '0 0 30px rgba(0,0,0,0.8)'
          }}>
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '14px' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                <Server size={18} color="#C89BED" />
                <span style={{ fontSize: '13px', fontWeight: '800', color: '#FFFFFF' }}>KASHTRIX CG GATEWAY SETTINGS</span>
              </div>
              <button onClick={() => setShowConfigModal(false)} style={{ background: 'none', border: 'none', color: '#94A3B8', cursor: 'pointer', fontSize: '13px' }}>✕</button>
            </div>

            <div style={{ marginBottom: '14px' }}>
              <label style={{ fontSize: '10.5px', color: '#94A3B8', display: 'block', marginBottom: '4px' }}>
                API GATEWAY REST/WS URL:
              </label>
              <input
                type="text"
                value={apiBaseUrl}
                onChange={e => setApiBaseUrl(e.target.value)}
                style={{
                  width: '100%',
                  background: '#06090E',
                  border: '1px solid #1E293B',
                  borderRadius: '4px',
                  padding: '7px',
                  color: '#FFFFFF',
                  fontSize: '11px',
                  outline: 'none'
                }}
              />
              <span style={{ fontSize: '9.5px', color: '#64748B', marginTop: '3px', display: 'block' }}>
                Default: http://127.0.0.1:9080 (Kashtrix.ApiGateway)
              </span>
            </div>

            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '8px' }}>
              <button
                onClick={() => setShowConfigModal(false)}
                style={{ padding: '6px 12px', borderRadius: '4px', background: '#161F2E', color: '#94A3B8', fontSize: '11px' }}
              >
                Cancel
              </button>
              <button
                onClick={() => {
                  setApiBase(apiBaseUrl);
                  setShowConfigModal(false);
                }}
                style={{ padding: '6px 14px', borderRadius: '4px', background: '#6E45A1', color: '#FFFFFF', fontWeight: '700', fontSize: '11px' }}
              >
                Save & Connect
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
