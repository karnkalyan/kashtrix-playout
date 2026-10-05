import React, { useState, useEffect, useRef } from 'react';
import {
  Play, Pause, Square, SkipForward, RotateCcw, Volume2, VolumeX,
  Tv, Clock, Settings, RefreshCw, Film, ShieldCheck,
  ChevronRight, CornerDownRight, CheckCircle2, AlertCircle, Info,
  Sliders, FastForward, Rewind, Layers, Search, Radio, Video,
  Camera, Globe, Monitor, Mic, Sparkles, X, ChevronDown, Download,
  SlidersHorizontal, Activity, Cpu, HardDrive, Bell
} from 'lucide-react';
import { PlayoutApi, getApiBase, setApiBase, getMediaUrl } from './api';

export default function PlayoutApp() {
  // Dynamic API State
  const [channels, setChannels] = useState([]);
  const [activeChannelId, setActiveChannelId] = useState('');
  const [channelStatus, setChannelStatus] = useState(null);
  const [playlist, setPlaylist] = useState([]);
  const [isApiConnected, setIsApiConnected] = useState(false);
  const [apiLatency, setApiLatency] = useState(null);
  const [apiBaseUrl, setApiBaseUrl] = useState(getApiBase());

  // Playback & UI Selection State
  const [isPlaying, setIsPlaying] = useState(false);
  const [selectedRowId, setSelectedRowId] = useState('');
  const [selectedCategory, setSelectedCategory] = useState('ALL');
  const [searchFilter, setSearchFilter] = useState('');
  const [showSafeGuides, setShowSafeGuides] = useState(true);
  const [masterClock, setMasterClock] = useState('');
  const [currentProgress, setCurrentProgress] = useState(0);

  // Audio & VU Controls
  const [audioMuted, setAudioMuted] = useState(false);
  const [masterVolume, setMasterVolume] = useState(85);
  const [vuLevels, setVuLevels] = useState({ left: -48, right: -48, master: -48 });

  // Preview Bus Controls
  const [previewSeek, setPreviewSeek] = useState(0);
  const [previewPlaying, setPreviewPlaying] = useState(false);

  // Bottom Quick Controls & Inputs Dock State
  const [showBottomDock, setShowBottomDock] = useState(true);
  const [bottomTab, setBottomTab] = useState('QUICK_CONTROLS'); // 'QUICK_CONTROLS' | 'LIVE_INPUTS' | 'CG_OVERLAYS' | 'SCTE_MACROS'

  // Top Menu Popups & Modal State
  const [openDropdown, setOpenDropdown] = useState(null); // 'FILE' | 'PLAYLIST' | 'SCHEDULE' | 'TOOLS' | 'SETTINGS' | 'HELP'
  const [activeModal, setActiveModal] = useState(null); // 'SCHEDULE' | 'INPUTS' | 'AUDIO_MIXER' | 'QC_PROCESSOR' | 'SETTINGS' | 'SHORTCUTS' | 'ABOUT' | 'EXPORT_EPG' | 'CONFIG'

  // Quick CG Overlays On-Air status
  const [cgOverlays, setCgOverlays] = useState([
    { id: 'cg-1', name: 'Station Bug / Clock', layer: 4, active: true, type: 'BUG' },
    { id: 'cg-2', name: 'Lower Third Headline', layer: 1, active: false, type: 'LOWER_THIRD' },
    { id: 'cg-3', name: 'Breaking News Banner', layer: 2, active: false, type: 'BREAKING' },
    { id: 'cg-4', name: '24/7 Live Ticker Crawl', layer: 6, active: true, type: 'TICKER' },
    { id: 'cg-5', name: 'Sports Scorebug', layer: 3, active: false, type: 'SCOREBUG' },
    { id: 'cg-6', name: 'Emergency Weather Alert', layer: 8, active: false, type: 'ALERT' }
  ]);

  // Live Input Sources
  const [liveSources] = useState([
    { id: 'inp-1', name: 'CAM 1 · Studio DirectShow', device: 'USB2.0 FHD UVC WebCam', format: '1080p50', type: 'WEBCAM', status: 'ACTIVE' },
    { id: 'inp-2', name: 'NDI 1 · Studio Switcher PGM', device: 'NDI://STUDIO-01/PGM', format: '1080p50', type: 'NDI', status: 'READY' },
    { id: 'inp-3', name: 'SDI 1 · DeckLink 8K Pro In', device: 'Blackmagic DeckLink 8K', format: '1080p50 10-bit', type: 'SDI', status: 'READY' },
    { id: 'inp-4', name: 'IP STREAM · Remote OB Van', device: 'rtmp://live.kashtrix.net/live/obvan', format: '1080p30 H.264', type: 'IP', status: 'ONLINE' },
    { id: 'inp-5', name: 'SCREEN · Master Control Desktop', device: 'Display 1 (1920x1080@60Hz)', format: '1080p60 RGB', type: 'SCREEN', status: 'READY' },
    { id: 'inp-6', name: 'TEST BARS · SMPTE HD Pattern', device: 'Internal Generator + 1kHz Tone', format: '1080p50 Test', type: 'GENERATOR', status: 'READY' }
  ]);

  // Broadcast Event Logs
  const [engineLogs, setEngineLogs] = useState([
    { time: '14:00:00', text: 'Kashtrix Playout Web Engine Initialized', level: 'SUCCESS' }
  ]);

  // Video Refs
  const programVideoRef = useRef(null);
  const previewVideoRef = useRef(null);

  // SMPTE Timecode Formatter (HH:MM:SS:FF at 25 fps)
  const formatSMPTE = (totalSec) => {
    const sec = Math.max(0, Math.floor(totalSec || 0));
    const h = String(Math.floor(sec / 3600)).padStart(2, '0');
    const m = String(Math.floor((sec % 3600) / 60)).padStart(2, '0');
    const s = String(sec % 60).padStart(2, '0');
    const f = String(Math.floor(((totalSec || 0) % 1) * 25)).padStart(2, '0');
    return `${h}:${m}:${s}:${f}`;
  };

  const addLog = (text, level = 'INFO') => {
    const now = new Date();
    const timeStr = `${String(now.getHours()).padStart(2, '0')}:${String(now.getMinutes()).padStart(2, '0')}:${String(now.getSeconds()).padStart(2, '0')}`;
    setEngineLogs(prev => [{ time: timeStr, text, level }, ...prev.slice(0, 40)]);
  };

  // Close dropdown menu on outside click
  useEffect(() => {
    const handleClickOutside = () => setOpenDropdown(null);
    if (openDropdown) {
      window.addEventListener('click', handleClickOutside);
    }
    return () => window.removeEventListener('click', handleClickOutside);
  }, [openDropdown]);

  // 1. Poll Real API Gateway (Channels, Status, Playlist)
  useEffect(() => {
    let isSubscribed = true;

    const pollApi = async () => {
      const startTime = performance.now();
      try {
        const health = await PlayoutApi.checkHealth();
        if (!isSubscribed) return;

        if (health && health.ok) {
          setIsApiConnected(true);
          setApiLatency(Math.round(performance.now() - startTime));

          let currentActiveChannel = activeChannelId;
          let currentChannelData = null;

          // Fetch actual channels from PlatformControlBus API
          try {
            const chRes = await PlayoutApi.getChannels();
            if (isSubscribed && chRes && chRes.ok && Array.isArray(chRes.data) && chRes.data.length > 0) {
              const normalized = chRes.data.map(c => ({
                channelId: c.channelId || c.ChannelId || 'KTX-PLAYOUT-01',
                channelName: c.channelName || c.ChannelName || 'Kashtrix Playout Main',
                engine: (c.engine || c.Engine || 'IDLE').toUpperCase(),
                currentItemId: c.currentItemId || c.CurrentItemId || '',
                currentTitle: c.currentTitle || c.CurrentTitle || '',
                selectedItemId: c.selectedItemId || c.SelectedItemId || '',
                selectedTitle: c.selectedTitle || c.SelectedTitle || '',
                elapsedSeconds: Number(c.elapsedSeconds !== undefined ? c.elapsedSeconds : (c.ElapsedSeconds !== undefined ? c.ElapsedSeconds : 0)),
                durationSeconds: Number(c.durationSeconds !== undefined ? c.durationSeconds : (c.DurationSeconds !== undefined ? c.DurationSeconds : 0)),
                outputArmed: c.outputArmed !== undefined ? c.outputArmed : (c.OutputArmed !== undefined ? c.OutputArmed : false),
                haStatus: c.haStatus || c.HaStatus || 'STANDALONE',
                capabilityStatus: c.capabilityStatus || c.CapabilityStatus || 'SDI · NDI · 1080p50',
                updatedUtc: c.updatedUtc || c.UpdatedUtc || ''
              }));

              setChannels(normalized);

              const targetId = currentActiveChannel || normalized[0].channelId;
              const active = normalized.find(c => c.channelId === targetId) || normalized[0];
              if (active) {
                currentChannelData = active;
                if (!currentActiveChannel || !normalized.some(c => c.channelId === currentActiveChannel)) {
                  currentActiveChannel = active.channelId;
                  setActiveChannelId(active.channelId);
                }
                setChannelStatus(active);
                setCurrentProgress(active.elapsedSeconds || 0);
                setIsPlaying(active.engine === 'PLAYING');
              }
            }
          } catch { }

          // Fetch actual playlist from database via API
          try {
            const channelToQuery = currentActiveChannel || activeChannelId || 'KTX-PLAYOUT-01';
            const plRes = await PlayoutApi.getPlaylist(channelToQuery);
            if (isSubscribed && plRes && plRes.ok && Array.isArray(plRes.data)) {
              const active = currentChannelData || channelStatus;
              const mapped = plRes.data.map((item, idx) => {
                const itemId = String(item.id || item.Id || `clip-${idx}`);
                const title = item.title || item.Title || (item.filePath || item.FilePath ? (item.filePath || item.FilePath).split(/[\\/]/).pop() : `Item #${idx + 1}`);
                const slug = item.slug || item.Slug || title;
                const durSec = Number(item.durationSeconds !== undefined ? item.durationSeconds : (item.DurationSeconds !== undefined ? item.DurationSeconds : 30));
                const durCode = item.duration || item.Duration || formatSMPTE(durSec);
                const eventType = item.eventType || item.EventType || (item.isLiveSource || item.IsLiveSource ? 'LIVE' : 'CLIP');
                const category = item.category || item.Category || 'Media';
                const codec = item.codec || item.Codec || 'h264';
                const videoFormat = item.videoFormat || item.VideoFormat || '1080p50';
                const inPoint = item.inPoint || item.InPoint || '00:00:00';
                const outPoint = item.outPoint || item.OutPoint || durCode;
                const source = item.filePath || item.FilePath || '';

                let status = (item.status || item.Status || 'READY').toUpperCase().replace(' ', '_');
                if (active) {
                  const isCurrent = (active.currentItemId && itemId.toLowerCase() === String(active.currentItemId).toLowerCase())
                    || (active.currentTitle && title === active.currentTitle);

                  const isCued = !isCurrent && (
                    (active.selectedItemId && itemId.toLowerCase() === String(active.selectedItemId).toLowerCase())
                    || (active.selectedTitle && title === active.selectedTitle)
                  );

                  if (isCurrent) status = 'ON_AIR';
                  else if (isCued) status = 'CUED';
                }

                return {
                  id: itemId,
                  sequence: item.sequence || item.Sequence || (idx + 1),
                  slug,
                  title,
                  type: eventType,
                  category,
                  codec,
                  videoFormat,
                  duration: durSec,
                  durationCode: durCode,
                  inPoint,
                  outPoint,
                  source,
                  status,
                  airTime: item.airTime || item.AirTime || '--:--:--',
                  transition: item.transition || 'CUT'
                };
              });

              setPlaylist(mapped);

              if (!selectedRowId && mapped.length > 0) {
                const onAir = mapped.find(m => m.status === 'ON_AIR');
                const cued = mapped.find(m => m.status === 'CUED');
                setSelectedRowId((cued || onAir || mapped[0]).id);
              }
            }
          } catch { }
        } else {
          setIsApiConnected(false);
        }
      } catch {
        if (isSubscribed) setIsApiConnected(false);
      }
    };

    pollApi();
    const timer = setInterval(pollApi, 1000);
    return () => {
      isSubscribed = false;
      clearInterval(timer);
    };
  }, [activeChannelId]);

  // 2. Master Clock & Audio VU Simulation
  useEffect(() => {
    const clockInterval = setInterval(() => {
      const now = new Date();
      const h = String(now.getHours()).padStart(2, '0');
      const m = String(now.getMinutes()).padStart(2, '0');
      const s = String(now.getSeconds()).padStart(2, '0');
      const f = String(Math.floor((now.getMilliseconds() / 1000) * 25)).padStart(2, '0');
      setMasterClock(`${h}:${m}:${s}:${f}`);

      if (isPlaying && !audioMuted) {
        const leftDb = -12 + (Math.sin(now.getTime() / 250) * 4);
        const rightDb = -12 + (Math.cos(now.getTime() / 280) * 4);
        const masterDb = Math.max(leftDb, rightDb);
        setVuLevels({
          left: Math.round(leftDb * 10) / 10,
          right: Math.round(rightDb * 10) / 10,
          master: Math.round(masterDb * 10) / 10
        });
      } else {
        setVuLevels({ left: -60, right: -60, master: -60 });
      }
    }, 100);

    return () => clearInterval(clockInterval);
  }, [isPlaying, audioMuted]);

  // Synchronize Program Video player with channel status progress
  useEffect(() => {
    if (programVideoRef.current && isPlaying) {
      const vid = programVideoRef.current;
      if (Math.abs(vid.currentTime - currentProgress) > 1.5) {
        vid.currentTime = currentProgress;
      }
      if (vid.paused) {
        vid.play().catch(() => {});
      }
    } else if (programVideoRef.current && !isPlaying) {
      if (!programVideoRef.current.paused) {
        programVideoRef.current.pause();
      }
    }
  }, [currentProgress, isPlaying]);

  // Synchronize master volume with Program Video
  useEffect(() => {
    if (programVideoRef.current) {
      programVideoRef.current.volume = masterVolume / 100;
      programVideoRef.current.muted = audioMuted;
    }
  }, [masterVolume, audioMuted]);

  // Resolve Current (ON AIR), Cued (PVW), and Selected Items
  const currentItem = playlist.find(i => i.status === 'ON_AIR') || null;
  const cuedItem = playlist.find(i => i.status === 'CUED') || (selectedRowId ? playlist.find(i => i.id === selectedRowId) : null) || playlist[0] || null;
  const selectedItem = playlist.find(i => i.id === selectedRowId) || cuedItem || currentItem;

  const currentItemIndex = currentItem ? playlist.findIndex(i => i.id === currentItem.id) : -1;
  const nextItem = currentItemIndex >= 0 && currentItemIndex + 1 < playlist.length ? playlist[currentItemIndex + 1] : (playlist.length > 1 ? playlist[1] : null);
  const upNextItem = currentItemIndex >= 0 && currentItemIndex + 2 < playlist.length ? playlist[currentItemIndex + 2] : null;

  // Authoritative Timers
  const totalDuration = (channelStatus?.durationSeconds > 0 ? channelStatus.durationSeconds : null) || currentItem?.duration || 0;
  const elapsedSeconds = currentProgress;
  const remainingSeconds = totalDuration > 0 ? Math.max(0, totalDuration - elapsedSeconds) : 0;
  const isEOMWarning = totalDuration > 0 && remainingSeconds > 0 && remainingSeconds <= 10;
  const progressPercent = totalDuration > 0 ? Math.min(100, Math.max(0, (elapsedSeconds / totalDuration) * 100)) : 0;

  // Cumulative Playlist Times
  const totalPlaylistDuration = playlist.reduce((acc, itm) => acc + (itm.duration || 0), 0);
  const playlistPlayedTime = playlist.reduce((acc, itm, idx) => {
    if (itm.status === 'ON_AIR') return acc + elapsedSeconds;
    if (idx < currentItemIndex) return acc + (itm.duration || 0);
    return acc;
  }, 0);
  const playlistRemainingTime = Math.max(0, totalPlaylistDuration - playlistPlayedTime);

  // Categories
  const categories = ['ALL', ...Array.from(new Set(playlist.map(i => i.category).filter(Boolean)))];

  // Filtered Playlist
  const filteredPlaylist = playlist.filter(item => {
    const matchesCat = selectedCategory === 'ALL' || item.category === selectedCategory;
    const matchesSearch = !searchFilter || item.title.toLowerCase().includes(searchFilter.toLowerCase()) || (item.slug && item.slug.toLowerCase().includes(searchFilter.toLowerCase()));
    return matchesCat && matchesSearch;
  });

  // Playout Actions
  const handleTakeSelected = async () => {
    const targetId = cuedItem ? cuedItem.id : (selectedItem ? selectedItem.id : '');
    if (!targetId) return;
    addLog(`TAKE item "${cuedItem?.title || targetId}" to AIR...`, 'WARN');
    const res = await PlayoutApi.take(activeChannelId, targetId);
    if (res && res.ok) addLog(`TAKE executed successfully on ${activeChannelId}.`, 'SUCCESS');
  };

  const handleTakeNext = async () => {
    addLog(`TAKE NEXT event on ${activeChannelId}...`, 'WARN');
    const res = await PlayoutApi.takeNext(activeChannelId);
    if (res && res.ok) addLog(`TAKE NEXT executed on ${activeChannelId}.`, 'SUCCESS');
  };

  const handlePlay = async () => {
    addLog(`PLAY / RESUME program on ${activeChannelId}...`, 'INFO');
    const res = await PlayoutApi.resume(activeChannelId);
    if (res && res.ok) setIsPlaying(true);
  };

  const handlePause = async () => {
    addLog(`PAUSE program on ${activeChannelId}...`, 'INFO');
    const res = await PlayoutApi.pause(activeChannelId);
    if (res && res.ok) setIsPlaying(false);
  };

  const handleStop = async () => {
    addLog(`STOP program on ${activeChannelId}...`, 'WARN');
    const res = await PlayoutApi.stop(activeChannelId);
    if (res && res.ok) {
      setIsPlaying(false);
      setCurrentProgress(0);
    }
  };

  const handleCueItem = async (itemId) => {
    addLog(`CUE item ${itemId} to PREVIEW...`, 'INFO');
    setSelectedRowId(itemId);
    setPreviewSeek(0);
    if (previewVideoRef.current) previewVideoRef.current.currentTime = 0;
    await PlayoutApi.sendCommand(activeChannelId, 'cue', { itemId });
  };

  const handleSeekRelative = async (delta) => {
    const newPos = Math.max(0, Math.min(totalDuration, elapsedSeconds + delta));
    addLog(`Seeking program by ${delta > 0 ? '+' : ''}${delta}s to ${formatSMPTE(newPos)}...`, 'INFO');
    setCurrentProgress(newPos);
    if (programVideoRef.current) programVideoRef.current.currentTime = newPos;
    await PlayoutApi.sendCommand(activeChannelId, 'seek', { offsetSeconds: String(newPos) });
  };

  const handleGoToIn = async () => {
    addLog(`Jump to IN point (00:00:00:00)...`, 'INFO');
    setCurrentProgress(0);
    if (programVideoRef.current) programVideoRef.current.currentTime = 0;
    await PlayoutApi.sendCommand(activeChannelId, 'seek', { offsetSeconds: '0' });
  };

  const handleReloadPlaylist = async () => {
    addLog(`Syncing rundown from database via API...`, 'INFO');
    await PlayoutApi.reloadPlaylist(activeChannelId);
  };

  const handleArmToggle = async () => {
    const nextState = !channelStatus?.outputArmed;
    addLog(`Setting SDI/NDI Output to ${nextState ? 'ARMED' : 'INHIBIT'}...`, 'WARN');
    await PlayoutApi.armOutput(activeChannelId, nextState);
    setChannelStatus(prev => prev ? { ...prev, outputArmed: nextState } : null);
  };

  const handleFadeToBlack = async () => {
    addLog(`FADE TO BLACK (FTB) executed on ${activeChannelId}.`, 'WARN');
    await PlayoutApi.stop(activeChannelId);
    setIsPlaying(false);
  };

  const handleToggleCgOverlay = (cgId) => {
    setCgOverlays(prev => prev.map(c => {
      if (c.id === cgId) {
        const next = !c.active;
        addLog(`CG Overlay "${c.name}" ${next ? 'ACTIVATED ON-AIR' : 'CLEARED FROM PROGRAM'}.`, next ? 'SUCCESS' : 'INFO');
        return { ...c, active: next };
      }
      return c;
    }));
  };

  const handleTakeLiveSource = (source) => {
    addLog(`Taking LIVE source "${source.name}" (${source.device}) directly to AIR!`, 'WARN');
    setIsPlaying(true);
  };

  const handleCueLiveSource = (source) => {
    addLog(`Cued LIVE source "${source.name}" to PREVIEW bus.`, 'INFO');
  };

  const handleInsertLiveSource = (source) => {
    const newItem = {
      id: `live-${Date.now()}`,
      sequence: playlist.length + 1,
      slug: source.name,
      title: `LIVE: ${source.name}`,
      type: 'LIVE',
      category: 'Live',
      codec: 'Raw YUV',
      videoFormat: source.format,
      duration: 3600,
      durationCode: '01:00:00',
      inPoint: '00:00:00',
      outPoint: '01:00:00',
      source: source.device,
      status: 'READY',
      airTime: '--:--:--',
      transition: 'CUT'
    };
    setPlaylist(prev => [...prev, newItem]);
    addLog(`Live source "${source.name}" inserted into rundown.`, 'SUCCESS');
  };

  const handlePreviewPlayPause = () => {
    if (previewVideoRef.current) {
      if (previewPlaying) {
        previewVideoRef.current.pause();
        setPreviewPlaying(false);
      } else {
        previewVideoRef.current.play().then(() => setPreviewPlaying(true)).catch(() => {});
      }
    }
  };

  return (
    <div style={{
      display: 'flex',
      flexDirection: 'column',
      height: '100vh',
      width: '100vw',
      background: '#090D11',
      color: '#F4F6F8',
      fontFamily: 'Segoe UI, var(--font-sans)',
      overflow: 'hidden'
    }}>
      {/* 1. TOP MENU BAR (Interactive with Popups & Modals) */}
      <header style={{
        height: '38px',
        backgroundColor: '#0C1116',
        borderBottom: '1px solid #20272E',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        padding: '0 10px',
        flexShrink: 0,
        zIndex: 40,
        position: 'relative'
      }}>
        {/* Brand & Menu Strip */}
        <div style={{ display: 'flex', alignItems: 'center', gap: '16px' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <div style={{
              width: '24px',
              height: '24px',
              background: 'linear-gradient(135deg, #7C3AED, #4F46E5)',
              borderRadius: '4px',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              boxShadow: '0 0 10px rgba(124, 58, 237, 0.5)'
            }}>
              <Tv size={15} color="#FFFFFF" />
            </div>
            <span style={{ fontSize: '15px', fontWeight: '800', letterSpacing: '0.04em', color: '#F1F1F1' }}>
              KASHTRIX <span style={{ color: '#B78BE7' }}>PLAYOUT</span>
            </span>
          </div>

          {/* Interactive Menu Strip */}
          <nav style={{ display: 'flex', gap: '2px', fontSize: '12px', color: '#C8D0D6' }}>
            {/* FILE MENU */}
            <div style={{ position: 'relative' }}>
              <span
                onClick={(e) => { e.stopPropagation(); setOpenDropdown(openDropdown === 'FILE' ? null : 'FILE'); }}
                style={{ padding: '4px 8px', borderRadius: '3px', cursor: 'pointer', background: openDropdown === 'FILE' ? '#1C242D' : 'transparent' }}
              >
                File
              </span>
              {openDropdown === 'FILE' && (
                <div style={{
                  position: 'absolute', top: '28px', left: 0, background: '#121820', border: '1px solid #2B3744',
                  borderRadius: '4px', padding: '4px 0', minWidth: '220px', boxShadow: '0 8px 24px rgba(0,0,0,0.8)', zIndex: 100
                }}>
                  <div onClick={() => { setPlaylist([]); addLog('Created new blank playlist.', 'INFO'); setOpenDropdown(null); }} style={{ padding: '6px 14px', cursor: 'pointer', fontSize: '11.5px' }} onMouseEnter={e => e.currentTarget.style.background = '#1E2836'} onMouseLeave={e => e.currentTarget.style.background = 'transparent'}>New Playlist (Ctrl+N)</div>
                  <div onClick={() => { handleReloadPlaylist(); setOpenDropdown(null); }} style={{ padding: '6px 14px', cursor: 'pointer', fontSize: '11.5px' }} onMouseEnter={e => e.currentTarget.style.background = '#1E2836'} onMouseLeave={e => e.currentTarget.style.background = 'transparent'}>Open / Import Playlist (Clean)</div>
                  <div onClick={() => { setActiveModal('EXPORT_EPG'); setOpenDropdown(null); }} style={{ padding: '6px 14px', cursor: 'pointer', fontSize: '11.5px' }} onMouseEnter={e => e.currentTarget.style.background = '#1E2836'} onMouseLeave={e => e.currentTarget.style.background = 'transparent'}>Export EPG · XMLTV & JSON...</div>
                </div>
              )}
            </div>

            {/* PLAYLIST MENU */}
            <div style={{ position: 'relative' }}>
              <span
                onClick={(e) => { e.stopPropagation(); setOpenDropdown(openDropdown === 'PLAYLIST' ? null : 'PLAYLIST'); }}
                style={{ padding: '4px 8px', borderRadius: '3px', cursor: 'pointer', background: openDropdown === 'PLAYLIST' ? '#1C242D' : 'transparent' }}
              >
                Playlist
              </span>
              {openDropdown === 'PLAYLIST' && (
                <div style={{
                  position: 'absolute', top: '28px', left: 0, background: '#121820', border: '1px solid #2B3744',
                  borderRadius: '4px', padding: '4px 0', minWidth: '230px', boxShadow: '0 8px 24px rgba(0,0,0,0.8)', zIndex: 100
                }}>
                  <div onClick={() => { setActiveModal('INPUTS'); setOpenDropdown(null); }} style={{ padding: '6px 14px', cursor: 'pointer', fontSize: '11.5px' }} onMouseEnter={e => e.currentTarget.style.background = '#1E2836'} onMouseLeave={e => e.currentTarget.style.background = 'transparent'}>Add Live / Input Source...</div>
                  <div onClick={() => { setBottomTab('CG_OVERLAYS'); setShowBottomDock(true); setOpenDropdown(null); }} style={{ padding: '6px 14px', cursor: 'pointer', fontSize: '11.5px' }} onMouseEnter={e => e.currentTarget.style.background = '#1E2836'} onMouseLeave={e => e.currentTarget.style.background = 'transparent'}>Add CG Graphic Event...</div>
                  <div onClick={() => { handleReloadPlaylist(); setOpenDropdown(null); }} style={{ padding: '6px 14px', cursor: 'pointer', fontSize: '11.5px' }} onMouseEnter={e => e.currentTarget.style.background = '#1E2836'} onMouseLeave={e => e.currentTarget.style.background = 'transparent'}>Sync from Database</div>
                </div>
              )}
            </div>

            {/* SCHEDULE MENU */}
            <div style={{ position: 'relative' }}>
              <span
                onClick={() => { setActiveModal('SCHEDULE'); setOpenDropdown(null); }}
                style={{ padding: '4px 8px', borderRadius: '3px', cursor: 'pointer' }}
                onMouseEnter={e => e.currentTarget.style.background = '#1C242D'}
                onMouseLeave={e => e.currentTarget.style.background = 'transparent'}
              >
                Schedule & Timeline
              </span>
            </div>

            {/* TOOLS MENU */}
            <div style={{ position: 'relative' }}>
              <span
                onClick={(e) => { e.stopPropagation(); setOpenDropdown(openDropdown === 'TOOLS' ? null : 'TOOLS'); }}
                style={{ padding: '4px 8px', borderRadius: '3px', cursor: 'pointer', background: openDropdown === 'TOOLS' ? '#1C242D' : 'transparent' }}
              >
                Tools
              </span>
              {openDropdown === 'TOOLS' && (
                <div style={{
                  position: 'absolute', top: '28px', left: 0, background: '#121820', border: '1px solid #2B3744',
                  borderRadius: '4px', padding: '4px 0', minWidth: '240px', boxShadow: '0 8px 24px rgba(0,0,0,0.8)', zIndex: 100
                }}>
                  <div onClick={() => { window.open('http://127.0.0.1:5174', '_blank'); setOpenDropdown(null); }} style={{ padding: '6px 14px', cursor: 'pointer', fontSize: '11.5px' }} onMouseEnter={e => e.currentTarget.style.background = '#1E2836'} onMouseLeave={e => e.currentTarget.style.background = 'transparent'}>Open CG Controller (Web)</div>
                  <div onClick={() => { setActiveModal('AUDIO_MIXER'); setOpenDropdown(null); }} style={{ padding: '6px 14px', cursor: 'pointer', fontSize: '11.5px' }} onMouseEnter={e => e.currentTarget.style.background = '#1E2836'} onMouseLeave={e => e.currentTarget.style.background = 'transparent'}>Broadcast Audio Mixer...</div>
                  <div onClick={() => { setActiveModal('QC_PROCESSOR'); setOpenDropdown(null); }} style={{ padding: '6px 14px', cursor: 'pointer', fontSize: '11.5px' }} onMouseEnter={e => e.currentTarget.style.background = '#1E2836'} onMouseLeave={e => e.currentTarget.style.background = 'transparent'}>Video Processor &amp; QC...</div>
                </div>
              )}
            </div>

            {/* SETTINGS MENU */}
            <div style={{ position: 'relative' }}>
              <span
                onClick={() => { setActiveModal('SETTINGS'); setOpenDropdown(null); }}
                style={{ padding: '4px 8px', borderRadius: '3px', cursor: 'pointer' }}
                onMouseEnter={e => e.currentTarget.style.background = '#1C242D'}
                onMouseLeave={e => e.currentTarget.style.background = 'transparent'}
              >
                Settings
              </span>
            </div>

            {/* HELP MENU */}
            <div style={{ position: 'relative' }}>
              <span
                onClick={(e) => { e.stopPropagation(); setOpenDropdown(openDropdown === 'HELP' ? null : 'HELP'); }}
                style={{ padding: '4px 8px', borderRadius: '3px', cursor: 'pointer', background: openDropdown === 'HELP' ? '#1C242D' : 'transparent' }}
              >
                Help
              </span>
              {openDropdown === 'HELP' && (
                <div style={{
                  position: 'absolute', top: '28px', left: 0, background: '#121820', border: '1px solid #2B3744',
                  borderRadius: '4px', padding: '4px 0', minWidth: '200px', boxShadow: '0 8px 24px rgba(0,0,0,0.8)', zIndex: 100
                }}>
                  <div onClick={() => { setActiveModal('SHORTCUTS'); setOpenDropdown(null); }} style={{ padding: '6px 14px', cursor: 'pointer', fontSize: '11.5px' }} onMouseEnter={e => e.currentTarget.style.background = '#1E2836'} onMouseLeave={e => e.currentTarget.style.background = 'transparent'}>Keyboard Shortcuts (F5/F6/F8)</div>
                  <div onClick={() => { setActiveModal('ABOUT'); setOpenDropdown(null); }} style={{ padding: '6px 14px', cursor: 'pointer', fontSize: '11.5px' }} onMouseEnter={e => e.currentTarget.style.background = '#1E2836'} onMouseLeave={e => e.currentTarget.style.background = 'transparent'}>About Kashtrix Playout</div>
                </div>
              )}
            </div>
          </nav>
        </div>

        {/* Center: Master Station Clock */}
        <div style={{
          display: 'flex',
          alignItems: 'center',
          gap: '8px',
          background: '#070A0D',
          border: '1px solid #28313A',
          padding: '2px 14px',
          borderRadius: '3px'
        }}>
          <Clock size={13} color="#E9C91E" />
          <span style={{ fontSize: '10px', color: '#88939B', letterSpacing: '0.08em' }}>STATION TIME:</span>
          <span style={{
            fontSize: '15px',
            fontWeight: '800',
            fontFamily: 'var(--font-mono)',
            color: '#E9C91E',
            letterSpacing: '0.05em'
          }}>
            {masterClock || '00:00:00:00'}
          </span>
        </div>

        {/* Right: Active Channel Selector & Badges */}
        <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
          {/* Dynamic Channels dropdown */}
          <div style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
            <span style={{ fontSize: '10px', color: '#7E8A94' }}>CHANNEL:</span>
            <select
              value={activeChannelId}
              onChange={e => setActiveChannelId(e.target.value)}
              style={{
                background: '#161D24',
                color: '#FFFFFF',
                border: '1px solid #2B3540',
                padding: '3px 8px',
                borderRadius: '3px',
                fontSize: '11px',
                fontWeight: '700',
                outline: 'none',
                cursor: 'pointer'
              }}
            >
              {channels.map(c => (
                <option key={c.channelId} value={c.channelId}>
                  {c.channelName ? `${c.channelName} (${c.channelId})` : c.channelId}
                </option>
              ))}
            </select>
          </div>

          {/* Output Armed Badge */}
          <button
            onClick={handleArmToggle}
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: '5px',
              padding: '3px 9px',
              borderRadius: '3px',
              fontSize: '10.5px',
              fontWeight: '700',
              cursor: 'pointer',
              background: channelStatus?.outputArmed ? 'rgba(239, 68, 68, 0.2)' : '#12181E',
              border: channelStatus?.outputArmed ? '1px solid #EF4444' : '1px solid #2B3540',
              color: channelStatus?.outputArmed ? '#EF4444' : '#88939B'
            }}
          >
            <ShieldCheck size={12} />
            {channelStatus?.outputArmed ? 'SDI: ARMED' : 'SDI: INHIBIT'}
          </button>

          {/* Safe Guides Toggle */}
          <button
            onClick={() => setShowSafeGuides(!showSafeGuides)}
            style={{
              padding: '3px 8px',
              borderRadius: '3px',
              fontSize: '10.5px',
              fontWeight: '600',
              cursor: 'pointer',
              background: showSafeGuides ? '#1E2631' : '#12181E',
              border: '1px solid #2B3540',
              color: showSafeGuides ? '#38BDF8' : '#77838D'
            }}
          >
            SAFE GUIDES
          </button>

          {/* API Status Badge */}
          <div
            onClick={() => setActiveModal('CONFIG')}
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: '5px',
              padding: '3px 8px',
              borderRadius: '3px',
              fontSize: '10.5px',
              fontWeight: '700',
              background: isApiConnected ? 'rgba(16, 185, 129, 0.15)' : 'rgba(245, 158, 11, 0.15)',
              border: `1px solid ${isApiConnected ? '#10B981' : '#F59E0B'}`,
              color: isApiConnected ? '#10B981' : '#F59E0B',
              cursor: 'pointer'
            }}
            title="Configure API Gateway Connection"
          >
            <span style={{
              width: '6px',
              height: '6px',
              borderRadius: '50%',
              backgroundColor: isApiConnected ? '#10B981' : '#F59E0B',
              boxShadow: `0 0 6px ${isApiConnected ? '#10B981' : '#F59E0B'}`
            }} />
            {isApiConnected ? `ONLINE (${apiLatency}ms)` : 'CONNECTING...'}
          </div>

          <button
            onClick={() => setActiveModal('SETTINGS')}
            style={{
              padding: '4px',
              background: '#161D24',
              border: '1px solid #2B3540',
              borderRadius: '3px',
              color: '#89959F',
              cursor: 'pointer'
            }}
          >
            <Settings size={13} />
          </button>
        </div>
      </header>

      {/* 2. TOP OPERATOR BUS (4 COLUMNS: PREVIEW + TIMERS/TRANSPORT + AUDIO + PROGRAM) */}
      <section style={{
        display: 'grid',
        gridTemplateColumns: '390px 1fr 125px 390px',
        gap: '5px',
        padding: '5px',
        backgroundColor: '#090D11',
        borderBottom: '1px solid #20272E',
        height: '265px',
        minHeight: '265px',
        maxHeight: '265px',
        flexShrink: 0
      }}>
        {/* === COLUMN 1: PREVIEW BUS (Width 390px) === */}
        <div style={{
          display: 'flex',
          flexDirection: 'column',
          background: '#06090C',
          border: '1px solid #27303A',
          borderRadius: '3px',
          overflow: 'hidden'
        }}>
          {/* Preview Monitor Screen Surface */}
          <div style={{
            flex: 1,
            position: 'relative',
            background: '#000000',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            overflow: 'hidden'
          }}>
            {cuedItem && cuedItem.source ? (
              <video
                ref={previewVideoRef}
                key={`pvw-${cuedItem.id}`}
                src={getMediaUrl(cuedItem.source)}
                muted
                playsInline
                style={{
                  width: '100%',
                  height: '100%',
                  objectFit: 'contain',
                  background: '#000000'
                }}
                onTimeUpdate={() => {
                  if (previewVideoRef.current) setPreviewSeek(previewVideoRef.current.currentTime);
                }}
              />
            ) : (
              <div style={{ textAlign: 'center', opacity: 0.6 }}>
                <Film size={32} color="#B78BE7" style={{ opacity: 0.5, marginBottom: '4px' }} />
                <div style={{ fontSize: '11px', color: '#9AA4AB' }}>PREVIEW BUS STANDBY</div>
              </div>
            )}

            {/* Title Safe Guides Overlay */}
            {showSafeGuides && (
              <div style={{
                position: 'absolute',
                inset: '5%',
                border: '1px dashed rgba(255, 255, 255, 0.2)',
                pointerEvents: 'none'
              }}>
                <div style={{
                  position: 'absolute',
                  inset: '5%',
                  border: '1px solid rgba(255, 255, 255, 0.25)',
                  pointerEvents: 'none'
                }} />
              </div>
            )}

            {/* PREVIEW Purple Badge */}
            <div style={{
              position: 'absolute',
              top: '6px',
              left: '6px',
              background: '#47266D',
              border: '1px solid #8D55B7',
              color: '#FFFFFF',
              fontSize: '10px',
              fontWeight: '800',
              padding: '2px 7px',
              borderRadius: '2px',
              letterSpacing: '0.06em'
            }}>
              PREVIEW
            </div>
          </div>

          {/* Preview Sub-bar */}
          <div style={{
            height: '28px',
            background: '#11171C',
            borderTop: '1px solid #1D242C',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            padding: '0 8px'
          }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '6px', maxWidth: '230px', overflow: 'hidden' }}>
              <span style={{ fontSize: '11px', fontWeight: '700', color: '#C89BED' }}>PREVIEW</span>
              <span style={{ fontSize: '10px', color: '#68737C' }}>·</span>
              <span style={{ fontSize: '11px', color: '#E1E6EB', textOverflow: 'ellipsis', overflow: 'hidden', whiteSpace: 'nowrap' }}>
                {cuedItem?.title || 'NO CUE'}
              </span>
            </div>
            <div style={{
              fontFamily: 'var(--font-mono)',
              fontSize: '11.5px',
              fontWeight: '700',
              color: '#C89BED'
            }}>
              {formatSMPTE(previewSeek)} <span style={{ color: '#66717A' }}>/</span> <span style={{ color: '#9C86B1' }}>{cuedItem?.durationCode || '00:00:00:00'}</span>
            </div>
          </div>

          {/* Preview Bottom Bar: CUE + Scrub Slider + Play/Pause */}
          <div style={{
            height: '36px',
            background: '#0D1318',
            borderTop: '1px solid #1A2127',
            display: 'flex',
            alignItems: 'center',
            gap: '8px',
            padding: '0 8px'
          }}>
            <button
              onClick={() => cuedItem && handleCueItem(cuedItem.id)}
              style={{
                height: '24px',
                padding: '0 10px',
                background: '#1E2631',
                border: '1px solid #303A45',
                color: '#C8D0D6',
                borderRadius: '3px',
                fontSize: '10px',
                fontWeight: '700',
                cursor: 'pointer'
              }}
              title="Cue selected item to preview"
            >
              CUE
            </button>

            {/* Preview Seek Slider */}
            <input
              type="range"
              min="0"
              max={cuedItem?.duration || 100}
              step="0.5"
              value={previewSeek}
              onChange={(e) => {
                const val = parseFloat(e.target.value);
                setPreviewSeek(val);
                if (previewVideoRef.current) previewVideoRef.current.currentTime = val;
              }}
              style={{
                flex: 1,
                cursor: 'pointer',
                accentColor: '#9333EA',
                height: '4px'
              }}
            />

            <button
              onClick={handlePreviewPlayPause}
              style={{
                height: '24px',
                width: '30px',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                background: '#1E2631',
                border: '1px solid #303A45',
                color: '#C8D0D6',
                borderRadius: '3px',
                cursor: 'pointer'
              }}
              title="Play/Pause PREVIEW only"
            >
              {previewPlaying ? <Pause size={12} /> : <Play size={12} />}
            </button>
          </div>
        </div>

        {/* === COLUMN 2: TIMERS & TRANSPORT RUNTIME DECK (Center, exact Kashtrix) === */}
        <div style={{
          display: 'flex',
          flexDirection: 'column',
          background: '#0C1116',
          border: '1px solid #27303A',
          borderRadius: '3px',
          padding: '5px',
          gap: '5px'
        }}>
          {/* Dual Big Broadcast Clocks */}
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '5px', height: '94px' }}>
            {/* Current Played Time Card */}
            <div style={{
              background: '#11161B',
              border: '1px solid #30383F',
              borderRadius: '2px',
              display: 'flex',
              flexDirection: 'column',
              alignItems: 'center',
              justifyContent: 'center',
              padding: '3px'
            }}>
              <span style={{ fontSize: '11px', fontWeight: '600', color: '#D4D7DA', letterSpacing: '0.04em' }}>
                Current Played Time
              </span>
              <span style={{
                fontSize: '34px',
                fontWeight: '900',
                fontFamily: 'var(--font-mono)',
                color: '#4F9CE6',
                letterSpacing: '0.04em',
                lineHeight: 1.1
              }}>
                {formatSMPTE(elapsedSeconds)}
              </span>
            </div>

            {/* Current Remaining Time Card */}
            <div style={{
              background: '#11161B',
              border: '1px solid #30383F',
              borderRadius: '2px',
              display: 'flex',
              flexDirection: 'column',
              alignItems: 'center',
              justifyContent: 'center',
              padding: '3px'
            }}>
              <span style={{ fontSize: '11px', fontWeight: '600', color: '#D4D7DA', letterSpacing: '0.04em' }}>
                Current Remaining Time
              </span>
              <span style={{
                fontSize: '34px',
                fontWeight: '900',
                fontFamily: 'var(--font-mono)',
                color: isEOMWarning ? '#FF4444' : '#E45B61',
                letterSpacing: '0.04em',
                lineHeight: 1.1
              }}>
                -{formatSMPTE(remainingSeconds)}
              </span>
            </div>
          </div>

          {/* Progress Bar */}
          <div style={{
            height: '7px',
            background: '#1E2227',
            borderRadius: '2px',
            overflow: 'hidden',
            position: 'relative'
          }}>
            <div style={{
              height: '100%',
              width: `${progressPercent}%`,
              background: isEOMWarning ? 'linear-gradient(90deg, #E45B61, #FF2222)' : 'linear-gradient(90deg, #2563EB, #4F9CE6)',
              transition: 'width 0.2s linear'
            }} />
          </div>

          {/* 8 Kashtrix Transport Buttons */}
          <div style={{
            display: 'grid',
            gridTemplateColumns: 'repeat(8, 1fr)',
            gap: '4px',
            height: '48px'
          }}>
            <button onClick={handleGoToIn} style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', background: '#161E26', border: '1px solid #2B3744', color: '#D4DCE2', borderRadius: '3px', cursor: 'pointer' }} title="Go to IN point">
              <span style={{ fontSize: '12px', fontWeight: 'bold' }}>|◀</span>
              <span style={{ fontSize: '7.5px', fontWeight: '700', color: '#909BA4' }}>IN</span>
            </button>
            <button onClick={handleStop} style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', background: '#1E1719', border: '1px solid #482329', color: '#FF7B7B', borderRadius: '3px', cursor: 'pointer' }} title="Stop program">
              <Square size={13} fill="#FF7B7B" />
              <span style={{ fontSize: '7.5px', fontWeight: '700', color: '#FF7B7B', marginTop: '2px' }}>STOP</span>
            </button>
            <button onClick={isPlaying ? handlePause : handlePlay} style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', background: isPlaying ? '#281F12' : '#14251B', border: isPlaying ? '1px solid #634718' : '1px solid #1E5031', color: isPlaying ? '#F59E0B' : '#10B981', borderRadius: '3px', cursor: 'pointer' }} title={isPlaying ? 'Pause program' : 'Resume program'}>
              {isPlaying ? <Pause size={13} /> : <Play size={13} fill="#10B981" />}
              <span style={{ fontSize: '7.5px', fontWeight: '700', marginTop: '2px' }}>{isPlaying ? 'PAUSE' : 'RESUME'}</span>
            </button>
            <button onClick={handleTakeNext} style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', background: '#161E26', border: '1px solid #2B3744', color: '#D4DCE2', borderRadius: '3px', cursor: 'pointer' }} title="Next event">
              <span style={{ fontSize: '12px', fontWeight: 'bold' }}>▶|</span>
              <span style={{ fontSize: '7.5px', fontWeight: '700', color: '#909BA4' }}>NEXT</span>
            </button>
            <button onClick={() => handleSeekRelative(-5)} style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', background: '#161E26', border: '1px solid #2B3744', color: '#D4DCE2', borderRadius: '3px', cursor: 'pointer' }} title="Back 5 seconds">
              <span style={{ fontSize: '12px' }}>↶</span>
              <span style={{ fontSize: '7.5px', fontWeight: '700', color: '#909BA4' }}>-5 SEC</span>
            </button>
            <button onClick={handlePlay} style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', background: '#13281E', border: '1px solid #235438', color: '#4ADE80', borderRadius: '3px', cursor: 'pointer' }} title="Play program">
              <Play size={13} fill="#4ADE80" />
              <span style={{ fontSize: '7.5px', fontWeight: '700', color: '#4ADE80', marginTop: '2px' }}>PLAY</span>
            </button>
            <button onClick={() => handleSeekRelative(5)} style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', background: '#161E26', border: '1px solid #2B3744', color: '#D4DCE2', borderRadius: '3px', cursor: 'pointer' }} title="Forward 5 seconds">
              <span style={{ fontSize: '12px' }}>↷</span>
              <span style={{ fontSize: '7.5px', fontWeight: '700', color: '#909BA4' }}>+5 SEC</span>
            </button>
            <button onClick={handleTakeSelected} style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', background: 'linear-gradient(180deg, #7C3AED, #5B21B6)', border: '1px solid #A78BFA', color: '#FFFFFF', borderRadius: '3px', cursor: 'pointer', boxShadow: '0 0 10px rgba(124, 58, 237, 0.4)' }} title="Take selected / cued item to program">
              <span style={{ fontSize: '13px', fontWeight: '900' }}>✦</span>
              <span style={{ fontSize: '8.5px', fontWeight: '800', letterSpacing: '0.04em' }}>TAKE</span>
            </button>
          </div>

          {/* Bottom Sub-deck: 5 Broadcast Metrics */}
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(5, 1fr)', gap: '4px', height: '42px' }}>
            <div style={{ background: '#0F1419', border: '1px solid #303840', borderRadius: '2px', padding: '2px', textAlign: 'center' }}>
              <div style={{ fontSize: '7.5px', color: '#98A2A9' }}>Station Time</div>
              <div style={{ fontSize: '11px', fontWeight: '800', fontFamily: 'var(--font-mono)', color: '#E9C91E' }}>{masterClock.substring(0, 8) || '00:00:00'}</div>
            </div>
            <div style={{ background: '#0F1419', border: '1px solid #303840', borderRadius: '2px', padding: '2px', textAlign: 'center' }}>
              <div style={{ fontSize: '7.5px', color: '#98A2A9' }}>Playlist Played</div>
              <div style={{ fontSize: '11px', fontWeight: '800', fontFamily: 'var(--font-mono)', color: '#97C63E' }}>{formatSMPTE(playlistPlayedTime).substring(0, 8)}</div>
            </div>
            <div style={{ background: '#0F1419', border: '1px solid #303840', borderRadius: '2px', padding: '2px', textAlign: 'center' }}>
              <div style={{ fontSize: '7.5px', color: '#98A2A9' }}>Playlist Rem.</div>
              <div style={{ fontSize: '11px', fontWeight: '800', fontFamily: 'var(--font-mono)', color: '#F06A3C' }}>{formatSMPTE(playlistRemainingTime).substring(0, 8)}</div>
            </div>
            <div style={{ background: '#0F1419', border: '1px solid #303840', borderRadius: '2px', padding: '2px', textAlign: 'center' }}>
              <div style={{ fontSize: '7.5px', color: '#98A2A9' }}>Next Event</div>
              <div style={{ fontSize: '11px', fontWeight: '800', fontFamily: 'var(--font-mono)', color: '#D67CD9' }}>{nextItem?.durationCode?.substring(0, 8) || '00:00:00'}</div>
            </div>
            <div style={{ background: '#0F1419', border: '1px solid #303840', borderRadius: '2px', padding: '2px', textAlign: 'center' }}>
              <div style={{ fontSize: '7.5px', color: '#98A2A9' }}>Selected Rem.</div>
              <div style={{ fontSize: '11px', fontWeight: '800', fontFamily: 'var(--font-mono)', color: '#E33A4E' }}>{selectedItem?.durationCode?.substring(0, 8) || '00:00:00'}</div>
            </div>
          </div>
        </div>

        {/* === COLUMN 3: PROGRAM AUDIO PANEL (Width 125px) === */}
        <div style={{
          display: 'flex',
          flexDirection: 'column',
          background: '#0C1116',
          border: '1px solid #27303A',
          borderRadius: '3px',
          padding: '5px',
          overflow: 'hidden'
        }}>
          <div style={{ display: 'grid', gridTemplateColumns: '26px 1fr', fontSize: '8.5px', color: '#8C969E', textAlign: 'center', marginBottom: '3px' }}>
            <span>F</span>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr' }}>
              <span>L</span><span>R</span><span>M</span>
            </div>
          </div>

          <div style={{ flex: 1, display: 'grid', gridTemplateColumns: '26px 1fr', gap: '4px', minHeight: 0 }}>
            {/* Vertical Volume Fader */}
            <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', height: '100%' }}>
              <input
                type="range"
                min="0"
                max="100"
                value={masterVolume}
                onChange={e => setMasterVolume(parseInt(e.target.value))}
                style={{
                  writingMode: 'bt-lr',
                  WebkitAppearance: 'slider-vertical',
                  width: '16px',
                  height: '115px',
                  accentColor: '#38BDF8',
                  cursor: 'pointer'
                }}
              />
            </div>

            {/* Tri-Bar LED Peak Meters */}
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: '3px', background: '#070A0D', padding: '3px', borderRadius: '2px', border: '1px solid #1C242D', height: '100%' }}>
              <div style={{ background: '#111827', position: 'relative', borderRadius: '1px', overflow: 'hidden' }}>
                <div style={{ position: 'absolute', bottom: 0, left: 0, right: 0, height: `${Math.max(4, ((vuLevels.left + 60) / 60) * 100)}%`, background: vuLevels.left > -6 ? 'linear-gradient(to top, #10B981 70%, #F59E0B 85%, #EF4444 100%)' : 'linear-gradient(to top, #10B981, #F59E0B)', transition: 'height 0.08s ease-out' }} />
              </div>
              <div style={{ background: '#111827', position: 'relative', borderRadius: '1px', overflow: 'hidden' }}>
                <div style={{ position: 'absolute', bottom: 0, left: 0, right: 0, height: `${Math.max(4, ((vuLevels.right + 60) / 60) * 100)}%`, background: vuLevels.right > -6 ? 'linear-gradient(to top, #10B981 70%, #F59E0B 85%, #EF4444 100%)' : 'linear-gradient(to top, #10B981, #F59E0B)', transition: 'height 0.08s ease-out' }} />
              </div>
              <div style={{ background: '#111827', position: 'relative', borderRadius: '1px', overflow: 'hidden' }}>
                <div style={{ position: 'absolute', bottom: 0, left: 0, right: 0, height: `${Math.max(4, ((vuLevels.master + 60) / 60) * 100)}%`, background: vuLevels.master > -6 ? 'linear-gradient(to top, #10B981 70%, #F59E0B 85%, #EF4444 100%)' : 'linear-gradient(to top, #10B981, #38BDF8)', transition: 'height 0.08s ease-out' }} />
              </div>
            </div>
          </div>

          <div style={{ marginTop: '3px', display: 'flex', alignItems: 'center', justifyContent: 'space-between', fontSize: '8px', fontFamily: 'var(--font-mono)', color: '#8C969E' }}>
            <button onClick={() => setAudioMuted(!audioMuted)} style={{ background: audioMuted ? '#EF4444' : '#1E2631', border: 'none', color: '#FFF', borderRadius: '2px', padding: '2px 4px', cursor: 'pointer' }} title={audioMuted ? 'Unmute Audio' : 'Mute Audio'}>
              {audioMuted ? <VolumeX size={10} /> : <Volume2 size={10} />}
            </button>
            <span>{vuLevels.master.toFixed(1)} dB</span>
          </div>
        </div>

        {/* === COLUMN 4: PROGRAM BUS (Width 390px, Confidence Monitor) === */}
        <div style={{
          display: 'flex',
          flexDirection: 'column',
          background: '#06090C',
          border: '1px solid #3F1D24',
          borderRadius: '3px',
          overflow: 'hidden',
          boxShadow: isPlaying ? '0 0 16px rgba(239, 68, 68, 0.2)' : 'none'
        }}>
          {/* Program Monitor Screen Surface */}
          <div style={{
            flex: 1,
            position: 'relative',
            background: '#000000',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            overflow: 'hidden'
          }}>
            {currentItem && currentItem.source ? (
              <video
                ref={programVideoRef}
                key={`pgm-${currentItem.id}`}
                src={getMediaUrl(currentItem.source)}
                autoPlay={isPlaying}
                playsInline
                muted={audioMuted}
                style={{
                  width: '100%',
                  height: '100%',
                  objectFit: 'contain',
                  background: '#000000'
                }}
              />
            ) : (
              <div style={{ textAlign: 'center', opacity: 0.6 }}>
                <Film size={32} color="#EF4444" style={{ opacity: 0.5, marginBottom: '4px' }} />
                <div style={{ fontSize: '11px', color: '#9AA4AB' }}>PROGRAM BUS (STANDBY BLACK)</div>
              </div>
            )}

            {/* Title Safe Guides Overlay */}
            {showSafeGuides && (
              <div style={{
                position: 'absolute',
                inset: '5%',
                border: '1px dashed rgba(255, 255, 255, 0.2)',
                pointerEvents: 'none'
              }}>
                <div style={{
                  position: 'absolute',
                  inset: '5%',
                  border: '1px solid rgba(255, 255, 255, 0.25)',
                  pointerEvents: 'none'
                }} />
              </div>
            )}

            {/* ON AIR Red Tally Badge */}
            <div style={{
              position: 'absolute',
              top: '6px',
              left: '6px',
              background: '#B91C1C',
              border: '1px solid #EF4444',
              color: '#FFFFFF',
              fontSize: '10px',
              fontWeight: '800',
              padding: '2px 7px',
              borderRadius: '2px',
              letterSpacing: '0.06em',
              boxShadow: '0 0 8px rgba(239, 68, 68, 0.5)'
            }}>
              ● ON AIR (PROGRAM)
            </div>
          </div>

          {/* Program Sub-bar */}
          <div style={{
            height: '28px',
            background: '#11171C',
            borderTop: '1px solid #1D242C',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            padding: '0 8px'
          }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '6px', maxWidth: '230px', overflow: 'hidden' }}>
              <span style={{ fontSize: '11px', fontWeight: '700', color: '#FF9B9B' }}>PROGRAM</span>
              <span style={{ fontSize: '10px', color: '#68737C' }}>·</span>
              <span style={{ fontSize: '11px', color: '#E1E6EB', textOverflow: 'ellipsis', overflow: 'hidden', whiteSpace: 'nowrap' }}>
                {channelStatus?.currentTitle || currentItem?.title || 'OFF AIR'}
              </span>
            </div>
            <div style={{
              fontFamily: 'var(--font-mono)',
              fontSize: '11.5px',
              fontWeight: '700',
              color: '#4ADE80'
            }}>
              {formatSMPTE(elapsedSeconds)} <span style={{ color: '#66717A' }}>/</span> <span style={{ color: '#9AA4AB' }}>{formatSMPTE(totalDuration)}</span>
            </div>
          </div>

          {/* Program Bottom Bar */}
          <div style={{
            height: '36px',
            background: '#0D1318',
            borderTop: '1px solid #1A2127',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            padding: '0 8px'
          }}>
            <button
              onClick={handleTakeSelected}
              style={{
                height: '24px',
                padding: '0 12px',
                background: '#63378E',
                border: '1px solid #8D55B7',
                color: '#FFFFFF',
                borderRadius: '3px',
                fontSize: '10px',
                fontWeight: '800',
                cursor: 'pointer'
              }}
              title="Take preview to program"
            >
              TAKE
            </button>

            <div style={{ textAlign: 'center' }}>
              <div style={{ fontSize: '9px', fontWeight: '700', color: '#E1E6EB' }}>
                PROGRAM · {channelStatus?.channelId || activeChannelId}
              </div>
              <div style={{ fontSize: '8px', color: '#8C969E' }}>
                1080p50 · H.264 · Stereo
              </div>
            </div>

            <button
              onClick={isPlaying ? handlePause : handlePlay}
              style={{
                height: '24px',
                padding: '0 10px',
                background: '#1E2631',
                border: '1px solid #303A45',
                color: '#C8D0D6',
                borderRadius: '3px',
                fontSize: '10px',
                fontWeight: '700',
                cursor: 'pointer'
              }}
              title="Pause or Resume Program"
            >
              {isPlaying ? <Pause size={12} /> : <Play size={12} />}
            </button>
          </div>
        </div>
      </section>

      {/* 3. MIDDLE SECTION: PLAYLIST (WITH BLOCKS SIDEBAR) + RIGHT DOCK */}
      <div style={{
        flex: 1,
        display: 'grid',
        gridTemplateColumns: '1fr 310px',
        gap: '5px',
        padding: '5px 5px 0 5px',
        minHeight: 0,
        overflow: 'hidden'
      }}>
        {/* === LEFT AREA: PLAYLIST PANEL === */}
        <div style={{
          display: 'flex',
          flexDirection: 'column',
          background: '#090D11',
          border: '1px solid #27303A',
          borderRadius: '3px',
          minHeight: 0,
          overflow: 'hidden'
        }}>
          {/* Playlist Toolbar */}
          <div style={{
            height: '34px',
            background: '#0C1217',
            borderBottom: '1px solid #222A33',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            padding: '0 10px',
            flexShrink: 0
          }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
              <span style={{ fontSize: '11px', fontWeight: '700', color: '#B78BE7' }}>
                CURRENT PLAYLIST:
              </span>
              <span style={{ fontSize: '11px', color: '#E1E6EB', fontWeight: '600' }}>
                {channelStatus?.channelName || 'KASHTRIX BROADCAST RUNTIME'}
              </span>
              <span style={{
                fontSize: '10px',
                color: '#8C969E',
                background: '#151C24',
                padding: '2px 6px',
                borderRadius: '2px'
              }}>
                {playlist.length} ITEMS · TOTAL: {formatSMPTE(totalPlaylistDuration)}
              </span>
            </div>

            {/* Search Box & Action Buttons */}
            <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
              <div style={{
                display: 'flex',
                alignItems: 'center',
                background: '#080D11',
                border: '1px solid #27303A',
                borderRadius: '2px',
                padding: '0 6px',
                height: '24px'
              }}>
                <Search size={11} color="#68737C" style={{ marginRight: '4px' }} />
                <input
                  type="text"
                  placeholder="Filter rundown..."
                  value={searchFilter}
                  onChange={e => setSearchFilter(e.target.value)}
                  style={{
                    background: 'transparent',
                    border: 'none',
                    outline: 'none',
                    color: '#FFF',
                    fontSize: '10.5px',
                    width: '130px'
                  }}
                />
              </div>

              <button
                onClick={() => setActiveModal('SCHEDULE')}
                style={{
                  height: '24px',
                  padding: '0 8px',
                  background: '#1E2631',
                  border: '1px solid #303A45',
                  color: '#B78BE7',
                  borderRadius: '3px',
                  fontSize: '10px',
                  fontWeight: '600',
                  cursor: 'pointer',
                  display: 'flex',
                  alignItems: 'center',
                  gap: '4px'
                }}
                title="View visual broadcast timeline schedule"
              >
                ⏱ TIMELINE
              </button>

              <button
                onClick={() => setActiveModal('INPUTS')}
                style={{
                  height: '24px',
                  padding: '0 8px',
                  background: '#1E2631',
                  border: '1px solid #303A45',
                  color: '#38BDF8',
                  borderRadius: '3px',
                  fontSize: '10px',
                  fontWeight: '600',
                  cursor: 'pointer',
                  display: 'flex',
                  alignItems: 'center',
                  gap: '4px'
                }}
                title="Add Live Source (NDI, SDI, WebCam, URL)"
              >
                + LIVE INPUT
              </button>

              <button
                onClick={handleReloadPlaylist}
                style={{
                  height: '24px',
                  padding: '0 8px',
                  background: '#1E2631',
                  border: '1px solid #303A45',
                  color: '#C8D0D6',
                  borderRadius: '3px',
                  fontSize: '10px',
                  fontWeight: '600',
                  cursor: 'pointer',
                  display: 'flex',
                  alignItems: 'center',
                  gap: '4px'
                }}
                title="Reload from Broadcast Database"
              >
                <RefreshCw size={11} />
                SYNC DB
              </button>
            </div>
          </div>

          {/* Sub-layout: Blocks Sidebar (Left, 170px) + DataGrid Table (Right) */}
          <div style={{
            flex: 1,
            display: 'grid',
            gridTemplateColumns: '160px 1fr',
            minHeight: 0,
            overflow: 'hidden'
          }}>
            {/* BLOCKS / CATEGORIES PANEL */}
            <div style={{
              background: '#0A0F14',
              borderRight: '1px solid #26313A',
              display: 'flex',
              flexDirection: 'column',
              minHeight: 0,
              overflow: 'hidden'
            }}>
              <div style={{
                height: '26px',
                borderBottom: '1px solid #20272E',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
                padding: '0 8px',
                fontSize: '9.5px',
                fontWeight: '700',
                color: '#B78BE7'
              }}>
                <span>BLOCKS / CATEGORIES</span>
              </div>
              <div style={{ flex: 1, overflowY: 'auto', padding: '4px' }}>
                {categories.map(cat => {
                  const count = cat === 'ALL' ? playlist.length : playlist.filter(i => i.category === cat).length;
                  const isSel = selectedCategory === cat;
                  return (
                    <div
                      key={cat}
                      onClick={() => setSelectedCategory(cat)}
                      style={{
                        padding: '5px 8px',
                        borderRadius: '2px',
                        marginBottom: '2px',
                        cursor: 'pointer',
                        background: isSel ? '#1E2836' : 'transparent',
                        borderLeft: isSel ? '3px solid #38BDF8' : '3px solid transparent',
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'space-between'
                      }}
                    >
                      <span style={{ fontSize: '10.5px', fontWeight: isSel ? '700' : '500', color: isSel ? '#FFFFFF' : '#A3AFB7' }}>
                        {cat}
                      </span>
                      <span style={{ fontSize: '9px', color: '#68737C' }}>
                        {count}
                      </span>
                    </div>
                  );
                })}
              </div>
            </div>

            {/* RUNDOWN DATAGRID TABLE */}
            <div style={{ flex: 1, overflowY: 'auto', background: '#090D11', minHeight: 0 }}>
              <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '11px' }}>
                <thead style={{
                  position: 'sticky',
                  top: 0,
                  background: '#0E141B',
                  borderBottom: '1px solid #27303A',
                  zIndex: 10,
                  fontSize: '9.5px',
                  color: '#89959F',
                  textAlign: 'left'
                }}>
                  <tr>
                    <th style={{ width: '28px', padding: '5px 4px', textAlign: 'center' }}></th>
                    <th style={{ width: '35px', padding: '5px 4px' }}>#</th>
                    <th style={{ width: '85px', padding: '5px 6px' }}>START</th>
                    <th style={{ width: '85px', padding: '5px 6px' }}>DURATION</th>
                    <th style={{ width: '65px', padding: '5px 6px' }}>TYPE</th>
                    <th style={{ width: '85px', padding: '5px 6px' }}>CATEGORY</th>
                    <th style={{ padding: '5px 8px' }}>TITLE / DESCRIPTION</th>
                    <th style={{ width: '85px', padding: '5px 6px' }}>STATUS</th>
                    <th style={{ width: '75px', padding: '5px 6px' }}>IN</th>
                    <th style={{ width: '75px', padding: '5px 6px' }}>OUT</th>
                    <th style={{ width: '115px', padding: '5px 6px', textAlign: 'center' }}>COMMANDS</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredPlaylist.map((item, idx) => {
                    const isCurrentOnAir = item.status === 'ON_AIR';
                    const isCued = item.status === 'CUED';
                    const isSelected = selectedRowId === item.id;

                    let rowBg = '#090D11';
                    let borderLeft = '3px solid transparent';
                    if (isCurrentOnAir) {
                      rowBg = '#123D2B';
                      borderLeft = '3px solid #45D889';
                    } else if (isCued) {
                      rowBg = '#2F1E47';
                      borderLeft = '3px solid #B78BE7';
                    } else if (isSelected) {
                      rowBg = '#162231';
                      borderLeft = '3px solid #38BDF8';
                    } else if (idx % 2 === 1) {
                      rowBg = '#0B1015';
                    }

                    return (
                      <tr
                        key={item.id}
                        onClick={() => setSelectedRowId(item.id)}
                        onDoubleClick={() => handleCueItem(item.id)}
                        style={{
                          background: rowBg,
                          borderBottom: '1px solid #1E2630',
                          borderLeft,
                          cursor: 'pointer',
                          transition: 'background 0.1s'
                        }}
                      >
                        <td style={{ textAlign: 'center', padding: '5px 2px' }}>
                          {isCurrentOnAir ? (
                            <span style={{ color: '#45D889', fontSize: '10px' }}>▶</span>
                          ) : isCued ? (
                            <span style={{ color: '#B78BE7', fontSize: '10px' }}>●</span>
                          ) : null}
                        </td>
                        <td style={{ padding: '5px 4px', color: '#7E8B95', fontFamily: 'var(--font-mono)' }}>
                          {String(item.sequence).padStart(2, '0')}
                        </td>
                        <td style={{ padding: '5px 6px', fontFamily: 'var(--font-mono)', color: '#A0ABB4' }}>
                          {item.airTime}
                        </td>
                        <td style={{ padding: '5px 6px', fontFamily: 'var(--font-mono)', fontWeight: '700', color: isCurrentOnAir ? '#45D889' : '#C8D2DA' }}>
                          {item.durationCode}
                        </td>
                        <td style={{ padding: '5px 6px' }}>
                          <span style={{
                            padding: '1px 5px',
                            borderRadius: '2px',
                            fontSize: '9px',
                            fontWeight: '700',
                            background: item.type === 'LIVE' ? '#991B1B' : '#1E293B',
                            color: item.type === 'LIVE' ? '#FFF' : '#38BDF8'
                          }}>
                            {item.type}
                          </span>
                        </td>
                        <td style={{ padding: '5px 6px', color: '#B78BE7', fontSize: '10px' }}>
                          {item.category}
                        </td>
                        <td style={{ padding: '5px 8px' }}>
                          <div style={{ fontWeight: '600', color: isCurrentOnAir ? '#F4FFF8' : '#F1F1F1' }}>
                            {item.title}
                          </div>
                          <div style={{ fontSize: '9px', color: '#68737C', whiteSpace: 'nowrap', textOverflow: 'ellipsis', overflow: 'hidden', maxWidth: '360px' }}>
                            {item.source}
                          </div>
                        </td>
                        <td style={{ padding: '5px 6px' }}>
                          <span style={{
                            padding: '2px 6px',
                            borderRadius: '2px',
                            fontSize: '8.5px',
                            fontWeight: '800',
                            letterSpacing: '0.04em',
                            background: isCurrentOnAir ? '#10B981' : (isCued ? '#8B5CF6' : '#273344'),
                            color: '#FFFFFF'
                          }}>
                            {item.status}
                          </span>
                        </td>
                        <td style={{ padding: '5px 6px', fontFamily: 'var(--font-mono)', color: '#88949E' }}>
                          {item.inPoint}
                        </td>
                        <td style={{ padding: '5px 6px', fontFamily: 'var(--font-mono)', color: '#88949E' }}>
                          {item.outPoint}
                        </td>
                        <td style={{ padding: '5px 6px', textAlign: 'center' }}>
                          <div style={{ display: 'flex', gap: '4px', justifyContent: 'center' }}>
                            <button
                              onClick={(e) => { e.stopPropagation(); handleTakeSelected(); }}
                              style={{ padding: '2px 7px', background: '#63378E', border: '1px solid #8D55B7', color: '#FFF', borderRadius: '2px', fontSize: '9px', fontWeight: '700', cursor: 'pointer' }}
                              title="Take this item to AIR"
                            >
                              TAKE
                            </button>
                            <button
                              onClick={(e) => { e.stopPropagation(); handleCueItem(item.id); }}
                              style={{ padding: '2px 7px', background: '#1E2631', border: '1px solid #303A45', color: '#C8D0D6', borderRadius: '2px', fontSize: '9px', fontWeight: '700', cursor: 'pointer' }}
                              title="Cue this item to Preview"
                            >
                              CUE
                            </button>
                          </div>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          </div>
        </div>

        {/* === RIGHT SIDEBAR: MEDIA / EVENT DOCK === */}
        <aside style={{
          display: 'flex',
          flexDirection: 'column',
          gap: '5px',
          minHeight: 0,
          overflowY: 'auto'
        }}>
          {/* Card 1: NOW PLAYING */}
          <div style={{
            background: '#0F141A',
            border: '1px solid #27303A',
            borderRadius: '3px',
            padding: '7px'
          }}>
            <div style={{ fontSize: '9.5px', fontWeight: '700', color: '#45D889', marginBottom: '4px', display: 'flex', alignItems: 'center', gap: '5px' }}>
              <span>▣</span> NOW PLAYING
            </div>
            {currentItem ? (
              <div>
                <div style={{ fontSize: '11.5px', fontWeight: '700', color: '#FFF', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
                  {currentItem.title}
                </div>
                <div style={{ fontSize: '9.5px', color: '#9AA4AB', marginTop: '2px' }}>
                  {currentItem.type} · {currentItem.durationCode} · {currentItem.category}
                </div>
                <div style={{ fontSize: '8.5px', color: '#65717A', marginTop: '2px', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
                  {currentItem.source}
                </div>
              </div>
            ) : (
              <div style={{ fontSize: '11px', color: '#68737C' }}>No Item On Air (Standby)</div>
            )}
          </div>

          {/* Card 2: NEXT */}
          <div style={{
            background: '#0F141A',
            border: '1px solid #27303A',
            borderRadius: '3px',
            padding: '7px'
          }}>
            <div style={{ fontSize: '9.5px', fontWeight: '700', color: '#38BDF8', marginBottom: '4px' }}>
              NEXT
            </div>
            {nextItem ? (
              <div>
                <div style={{ fontSize: '11.5px', fontWeight: '700', color: '#FFF', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
                  {nextItem.title}
                </div>
                <div style={{ fontSize: '9.5px', color: '#9AA4AB', marginTop: '2px' }}>
                  {nextItem.type} · {nextItem.durationCode} · {nextItem.category}
                </div>
              </div>
            ) : (
              <div style={{ fontSize: '11px', color: '#68737C' }}>End of Rundown</div>
            )}
          </div>

          {/* Card 3: UP NEXT */}
          <div style={{
            background: '#0F141A',
            border: '1px solid #27303A',
            borderRadius: '3px',
            padding: '7px'
          }}>
            <div style={{ fontSize: '9.5px', fontWeight: '700', color: '#B78BE7', marginBottom: '4px' }}>
              UP NEXT
            </div>
            {upNextItem ? (
              <div>
                <div style={{ fontSize: '11.5px', fontWeight: '700', color: '#FFF', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
                  {upNextItem.title}
                </div>
                <div style={{ fontSize: '9.5px', color: '#9AA4AB', marginTop: '2px' }}>
                  {upNextItem.durationCode}
                </div>
              </div>
            ) : (
              <div style={{ fontSize: '11px', color: '#68737C' }}>--</div>
            )}
          </div>

          {/* Card 4: EVENT LOG */}
          <div style={{
            flex: 1,
            background: '#0F141A',
            border: '1px solid #27303A',
            borderRadius: '3px',
            padding: '7px',
            display: 'flex',
            flexDirection: 'column',
            minHeight: '110px'
          }}>
            <div style={{ fontSize: '9.5px', fontWeight: '700', color: '#B78BE7', marginBottom: '4px' }}>
              EVENT LOG
            </div>
            <div style={{ flex: 1, overflowY: 'auto', background: '#0B1015', borderRadius: '2px', padding: '4px' }}>
              {engineLogs.map((log, idx) => (
                <div key={idx} style={{ fontSize: '9px', marginBottom: '3px', display: 'flex', gap: '5px' }}>
                  <span style={{ color: '#68737C', fontFamily: 'var(--font-mono)' }}>{log.time}</span>
                  <span style={{
                    color: log.level === 'WARN' ? '#F59E0B' : (log.level === 'SUCCESS' ? '#45D889' : '#C8D2DA')
                  }}>
                    {log.text}
                  </span>
                </div>
              ))}
            </div>
          </div>
        </aside>
      </div>

      {/* 4. BOTTOM DOCK: QUICK CONTROLS, INPUT LIVE SOURCES, CG OVERLAYS, SCTE MACROS */}
      {showBottomDock && (
        <div style={{
          height: '148px',
          minHeight: '148px',
          maxHeight: '148px',
          background: '#0C1116',
          borderTop: '1px solid #20272E',
          display: 'flex',
          flexDirection: 'column',
          flexShrink: 0,
          margin: '0 5px',
          borderRadius: '3px 3px 0 0',
          overflow: 'hidden'
        }}>
          {/* Dock Header Tabs */}
          <div style={{
            height: '28px',
            background: '#080C10',
            borderBottom: '1px solid #1E2630',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            padding: '0 8px'
          }}>
            <div style={{ display: 'flex', gap: '2px' }}>
              {[
                { id: 'QUICK_CONTROLS', label: '⚡ QUICK CONTROLS' },
                { id: 'LIVE_INPUTS', label: '🔴 INPUT LIVE SOURCES (CAM / NDI / SDI / IP)' },
                { id: 'CG_OVERLAYS', label: '🎨 CG QUICK OVERLAYS & BUGS' },
                { id: 'SCTE_MACROS', label: '📡 SCTE-35 & COMMERCIAL MACROS' }
              ].map(tab => (
                <button
                  key={tab.id}
                  onClick={() => setBottomTab(tab.id)}
                  style={{
                    padding: '3px 10px',
                    fontSize: '10px',
                    fontWeight: '700',
                    border: 'none',
                    borderRadius: '2px 2px 0 0',
                    cursor: 'pointer',
                    background: bottomTab === tab.id ? '#1A232E' : 'transparent',
                    color: bottomTab === tab.id ? '#38BDF8' : '#7C8A96'
                  }}
                >
                  {tab.label}
                </button>
              ))}
            </div>
            <button
              onClick={() => setShowBottomDock(!showBottomDock)}
              style={{ background: 'transparent', border: 'none', color: '#68737C', cursor: 'pointer', fontSize: '10px' }}
              title="Minimize bottom dock"
            >
              ▼
            </button>
          </div>

          {/* Dock Tab Content */}
          <div style={{ flex: 1, padding: '6px 8px', overflowY: 'auto' }}>
            {/* TAB 1: QUICK CONTROLS */}
            {bottomTab === 'QUICK_CONTROLS' && (
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(7, 1fr)', gap: '6px', height: '100%' }}>
                <button
                  onClick={handleTakeSelected}
                  style={{ background: '#5B21B6', border: '1px solid #8B5CF6', color: '#FFF', borderRadius: '3px', padding: '6px', cursor: 'pointer', display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center' }}
                >
                  <Sparkles size={16} />
                  <span style={{ fontSize: '10px', fontWeight: '800', marginTop: '3px' }}>TAKE NOW (F5)</span>
                </button>

                <button
                  onClick={handleTakeNext}
                  style={{ background: '#1E293B', border: '1px solid #334155', color: '#38BDF8', borderRadius: '3px', padding: '6px', cursor: 'pointer', display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center' }}
                >
                  <SkipForward size={16} />
                  <span style={{ fontSize: '10px', fontWeight: '700', marginTop: '3px' }}>NEXT (F6)</span>
                </button>

                <button
                  onClick={handleFadeToBlack}
                  style={{ background: '#2B1214', border: '1px solid #7F1D1D', color: '#EF4444', borderRadius: '3px', padding: '6px', cursor: 'pointer', display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center' }}
                >
                  <Square size={16} fill="#EF4444" />
                  <span style={{ fontSize: '10px', fontWeight: '800', marginTop: '3px' }}>FADE TO BLACK</span>
                </button>

                <button
                  onClick={handleArmToggle}
                  style={{ background: channelStatus?.outputArmed ? '#064E3B' : '#1E293B', border: channelStatus?.outputArmed ? '1px solid #10B981' : '1px solid #334155', color: '#FFF', borderRadius: '3px', padding: '6px', cursor: 'pointer', display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center' }}
                >
                  <ShieldCheck size={16} />
                  <span style={{ fontSize: '10px', fontWeight: '700', marginTop: '3px' }}>{channelStatus?.outputArmed ? 'OUTPUT: ARMED' : 'ARM OUTPUT'}</span>
                </button>

                <button
                  onClick={() => setActiveModal('AUDIO_MIXER')}
                  style={{ background: '#1E293B', border: '1px solid #334155', color: '#CBD5E1', borderRadius: '3px', padding: '6px', cursor: 'pointer', display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center' }}
                >
                  <SlidersHorizontal size={16} />
                  <span style={{ fontSize: '10px', fontWeight: '700', marginTop: '3px' }}>AUDIO MIXER</span>
                </button>

                <button
                  onClick={() => setActiveModal('QC_PROCESSOR')}
                  style={{ background: '#1E293B', border: '1px solid #334155', color: '#CBD5E1', borderRadius: '3px', padding: '6px', cursor: 'pointer', display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center' }}
                >
                  <Activity size={16} />
                  <span style={{ fontSize: '10px', fontWeight: '700', marginTop: '3px' }}>QC &amp; PROCESSOR</span>
                </button>

                <button
                  onClick={handleReloadPlaylist}
                  style={{ background: '#1E293B', border: '1px solid #334155', color: '#A78BFA', borderRadius: '3px', padding: '6px', cursor: 'pointer', display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center' }}
                >
                  <RefreshCw size={16} />
                  <span style={{ fontSize: '10px', fontWeight: '700', marginTop: '3px' }}>SYNC DATABASE</span>
                </button>
              </div>
            )}

            {/* TAB 2: LIVE INPUT SOURCES */}
            {bottomTab === 'LIVE_INPUTS' && (
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(6, 1fr)', gap: '6px', height: '100%' }}>
                {liveSources.map(src => (
                  <div
                    key={src.id}
                    style={{
                      background: '#101620',
                      border: '1px solid #233040',
                      borderRadius: '3px',
                      padding: '5px',
                      display: 'flex',
                      flexDirection: 'column',
                      justifyContent: 'space-between'
                    }}
                  >
                    <div>
                      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                        <span style={{ fontSize: '9.5px', fontWeight: '800', color: src.type === 'WEBCAM' ? '#EF4444' : (src.type === 'NDI' ? '#38BDF8' : '#10B981') }}>
                          {src.type}
                        </span>
                        <span style={{ fontSize: '8px', color: '#68737C' }}>{src.format}</span>
                      </div>
                      <div style={{ fontSize: '10.5px', fontWeight: '700', color: '#FFF', marginTop: '2px', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
                        {src.name}
                      </div>
                    </div>
                    <div style={{ display: 'flex', gap: '3px', marginTop: '4px' }}>
                      <button
                        onClick={() => handleTakeLiveSource(src)}
                        style={{ flex: 1, padding: '2px 0', background: '#DC2626', border: 'none', color: '#FFF', borderRadius: '2px', fontSize: '9px', fontWeight: '800', cursor: 'pointer' }}
                        title="Take direct to Program Air"
                      >
                        TAKE
                      </button>
                      <button
                        onClick={() => handleCueLiveSource(src)}
                        style={{ flex: 1, padding: '2px 0', background: '#6D28D9', border: 'none', color: '#FFF', borderRadius: '2px', fontSize: '9px', fontWeight: '700', cursor: 'pointer' }}
                        title="Cue to Preview Bus"
                      >
                        CUE
                      </button>
                      <button
                        onClick={() => handleInsertLiveSource(src)}
                        style={{ flex: 1, padding: '2px 0', background: '#1E293B', border: '1px solid #334155', color: '#38BDF8', borderRadius: '2px', fontSize: '9px', fontWeight: '700', cursor: 'pointer' }}
                        title="Insert into Rundown"
                      >
                        +ADD
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            )}

            {/* TAB 3: CG OVERLAYS & BUGS */}
            {bottomTab === 'CG_OVERLAYS' && (
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(6, 1fr)', gap: '6px', height: '100%' }}>
                {cgOverlays.map(cg => (
                  <div
                    key={cg.id}
                    onClick={() => handleToggleCgOverlay(cg.id)}
                    style={{
                      background: cg.active ? '#1B263B' : '#101620',
                      border: cg.active ? '1px solid #38BDF8' : '1px solid #233040',
                      borderRadius: '3px',
                      padding: '6px',
                      cursor: 'pointer',
                      display: 'flex',
                      flexDirection: 'column',
                      justifyContent: 'space-between',
                      boxShadow: cg.active ? '0 0 10px rgba(56, 189, 248, 0.25)' : 'none'
                    }}
                  >
                    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                      <span style={{ fontSize: '9px', fontWeight: '800', color: '#B78BE7' }}>LAYER {cg.layer}</span>
                      <span style={{
                        padding: '1px 5px',
                        borderRadius: '2px',
                        fontSize: '8px',
                        fontWeight: '800',
                        background: cg.active ? '#10B981' : '#334155',
                        color: '#FFF'
                      }}>
                        {cg.active ? 'ON AIR' : 'OFF'}
                      </span>
                    </div>
                    <div style={{ fontSize: '11px', fontWeight: '700', color: '#FFF', margin: '4px 0' }}>
                      {cg.name}
                    </div>
                    <div style={{ fontSize: '8.5px', color: '#7E8B95' }}>
                      Click to {cg.active ? 'Kill' : 'Take to Air'}
                    </div>
                  </div>
                ))}
              </div>
            )}

            {/* TAB 4: SCTE-35 & COMMERCIAL MACROS */}
            {bottomTab === 'SCTE_MACROS' && (
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(6, 1fr)', gap: '6px', height: '100%' }}>
                {[
                  { label: 'SCTE-35 30s BREAK', desc: 'Splice Insert 30s Commercial', color: '#EA580C' },
                  { label: 'SCTE-35 60s BREAK', desc: 'Splice Insert 60s Break', color: '#EA580C' },
                  { label: 'SCTE-35 90s BREAK', desc: 'Splice Insert 90s Break', color: '#EA580C' },
                  { label: 'SCTE-104 ANC', desc: 'Insert VANC DPI Packet', color: '#6366F1' },
                  { label: 'GPI OPTICAL PULSE', desc: 'Pulse GPI Contact #1', color: '#10B981' },
                  { label: '1kHz CUE TONE', desc: 'Play 1000Hz Line Reference', color: '#F59E0B' }
                ].map((macro, idx) => (
                  <button
                    key={idx}
                    onClick={() => addLog(`Executed Macro: ${macro.label} (${macro.desc})`, 'SUCCESS')}
                    style={{
                      background: '#101620',
                      border: '1px solid #283545',
                      borderRadius: '3px',
                      padding: '6px',
                      cursor: 'pointer',
                      display: 'flex',
                      flexDirection: 'column',
                      alignItems: 'center',
                      justifyContent: 'center',
                      textAlign: 'center'
                    }}
                  >
                    <span style={{ fontSize: '10.5px', fontWeight: '800', color: macro.color }}>{macro.label}</span>
                    <span style={{ fontSize: '8px', color: '#88949E', marginTop: '2px' }}>{macro.desc}</span>
                  </button>
                ))}
              </div>
            )}
          </div>
        </div>
      )}

      {/* 5. BOTTOM STATUS BAR */}
      <footer style={{
        height: '30px',
        backgroundColor: '#0C1116',
        borderTop: '1px solid #20272E',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        padding: '0 12px',
        fontSize: '10.5px',
        color: '#89959F',
        flexShrink: 0
      }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '14px' }}>
          <span>CHANNEL: <strong style={{ color: '#FFF' }}>{channelStatus?.channelName || activeChannelId}</strong></span>
          <span>ENGINE: <strong style={{ color: isPlaying ? '#45D889' : '#F59E0B' }}>{channelStatus?.engine || (isPlaying ? 'PLAYING' : 'IDLE')}</strong></span>
          <span>CADENCE: <strong style={{ color: '#FFF' }}>50.00 fps (1080p50)</strong></span>
          <span>BUFFER: <strong style={{ color: '#45D889' }}>98%</strong></span>
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: '14px' }}>
          <span>AUDIO: <strong style={{ color: '#FFF' }}>STEREO 48kHz 24-bit</strong></span>
          <span>HA MODE: <strong style={{ color: '#B78BE7' }}>{channelStatus?.haStatus || 'STANDALONE'}</strong></span>
          <span>GATEWAY: <strong style={{ color: isApiConnected ? '#45D889' : '#F59E0B' }}>http://127.0.0.1:9080</strong></span>
        </div>
      </footer>

      {/* === MODALS & POPUPS (Triggered from Top Menus & Quick Controls) === */}

      {/* MODAL 1: SCHEDULE & RUNDOWN TIMELINE */}
      {activeModal === 'SCHEDULE' && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.8)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 100 }}>
          <div style={{ background: '#0F151C', border: '1px solid #2B3744', borderRadius: '6px', width: '850px', maxHeight: '80vh', display: 'flex', flexDirection: 'column', boxShadow: '0 0 30px rgba(0,0,0,0.9)' }}>
            <div style={{ padding: '12px 16px', borderBottom: '1px solid #222C38', display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
              <span style={{ fontSize: '14px', fontWeight: '800', color: '#B78BE7' }}>⏱ 24-HOUR BROADCAST RUNDOWN TIMELINE &amp; SCHEDULE</span>
              <button onClick={() => setActiveModal(null)} style={{ background: 'transparent', border: 'none', color: '#88949E', cursor: 'pointer' }}><X size={18} /></button>
            </div>
            <div style={{ flex: 1, padding: '16px', overflowY: 'auto' }}>
              <div style={{ background: '#090E13', border: '1px solid #232E37', borderRadius: '4px', padding: '10px', marginBottom: '14px' }}>
                <div style={{ fontSize: '11px', color: '#A0ABB4', marginBottom: '6px' }}>Broadcast Playhead Position: <strong>{masterClock}</strong></div>
                {/* Visual Hour Grid */}
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(12, 1fr)', gap: '2px', height: '50px', background: '#05070A', borderRadius: '3px', padding: '3px' }}>
                  {['14:00', '14:30', '15:00', '15:30', '16:00', '16:30', '17:00', '17:30', '18:00', '18:30', '19:00', '19:30'].map((time, idx) => (
                    <div key={idx} style={{ background: idx === 0 ? '#1E3A8A' : '#111827', border: idx === 0 ? '1px solid #3B82F6' : '1px solid #1F2937', borderRadius: '2px', padding: '4px', fontSize: '9px', textAlign: 'center' }}>
                      <div style={{ color: '#93C5FD', fontWeight: '700' }}>{time}</div>
                      <div style={{ color: '#6B7280', fontSize: '8px' }}>Block #{idx + 1}</div>
                    </div>
                  ))}
                </div>
              </div>
              <h4 style={{ fontSize: '12px', color: '#FFF', marginBottom: '8px' }}>Scheduled Events</h4>
              <div style={{ border: '1px solid #1E2836', borderRadius: '4px', overflow: 'hidden' }}>
                {playlist.map((itm, i) => (
                  <div key={itm.id} style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '8px 12px', background: i % 2 === 0 ? '#0B1015' : '#0E141B', borderBottom: '1px solid #1A232E' }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
                      <span style={{ fontFamily: 'var(--font-mono)', color: '#38BDF8', fontSize: '11px' }}>{itm.airTime}</span>
                      <span style={{ fontWeight: '600', color: '#FFF', fontSize: '12px' }}>{itm.title}</span>
                    </div>
                    <span style={{ fontFamily: 'var(--font-mono)', color: '#9AA4AB', fontSize: '11px' }}>{itm.durationCode}</span>
                  </div>
                ))}
              </div>
            </div>
            <div style={{ padding: '10px 16px', borderTop: '1px solid #222C38', display: 'flex', justifyContent: 'flex-end' }}>
              <button onClick={() => setActiveModal(null)} style={{ padding: '6px 16px', background: '#63378E', border: '1px solid #8D55B7', color: '#FFF', borderRadius: '3px', cursor: 'pointer', fontWeight: '700', fontSize: '11px' }}>Close</button>
            </div>
          </div>
        </div>
      )}

      {/* MODAL 2: BROADCAST AUDIO MIXER */}
      {activeModal === 'AUDIO_MIXER' && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.8)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 100 }}>
          <div style={{ background: '#0F151C', border: '1px solid #2B3744', borderRadius: '6px', width: '680px', padding: '16px', boxShadow: '0 0 30px rgba(0,0,0,0.9)' }}>
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '16px', borderBottom: '1px solid #222C38', paddingBottom: '8px' }}>
              <span style={{ fontSize: '14px', fontWeight: '800', color: '#38BDF8' }}>🎚 BROADCAST MULTI-CHANNEL AUDIO MIXER</span>
              <button onClick={() => setActiveModal(null)} style={{ background: 'transparent', border: 'none', color: '#88949E', cursor: 'pointer' }}><X size={18} /></button>
            </div>
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(5, 1fr)', gap: '12px', background: '#090E13', padding: '14px', borderRadius: '4px', border: '1px solid #1E2836' }}>
              {['PROGRAM BUS', 'PREVIEW BUS', 'LIVE CAM IN', 'NDI AUDIO', 'MASTER OUTPUT'].map((ch, idx) => (
                <div key={idx} style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '8px' }}>
                  <span style={{ fontSize: '10px', fontWeight: '700', color: idx === 4 ? '#E9C91E' : '#CBD5E1', textAlign: 'center' }}>{ch}</span>
                  <input type="range" min="0" max="100" defaultValue={idx === 4 ? masterVolume : 80} style={{ writingMode: 'bt-lr', WebkitAppearance: 'slider-vertical', height: '140px', width: '18px', accentColor: idx === 4 ? '#E9C91E' : '#38BDF8' }} />
                  <span style={{ fontSize: '9px', fontFamily: 'var(--font-mono)', color: '#8C969E' }}>-12.0 dB</span>
                  <div style={{ display: 'flex', gap: '4px' }}>
                    <button style={{ padding: '2px 5px', fontSize: '9px', fontWeight: '700', background: '#1E293B', border: '1px solid #334155', color: '#FFF', borderRadius: '2px', cursor: 'pointer' }}>M</button>
                    <button style={{ padding: '2px 5px', fontSize: '9px', fontWeight: '700', background: '#1E293B', border: '1px solid #334155', color: '#EAB308', borderRadius: '2px', cursor: 'pointer' }}>S</button>
                  </div>
                </div>
              ))}
            </div>
            <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: '14px' }}>
              <button onClick={() => setActiveModal(null)} style={{ padding: '6px 16px', background: '#1E293B', border: '1px solid #334155', color: '#FFF', borderRadius: '3px', cursor: 'pointer', fontSize: '11px', fontWeight: '700' }}>Close</button>
            </div>
          </div>
        </div>
      )}

      {/* MODAL 3: VIDEO PROCESSOR & QC */}
      {activeModal === 'QC_PROCESSOR' && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.8)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 100 }}>
          <div style={{ background: '#0F151C', border: '1px solid #2B3744', borderRadius: '6px', width: '640px', padding: '16px', boxShadow: '0 0 30px rgba(0,0,0,0.9)' }}>
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '14px', borderBottom: '1px solid #222C38', paddingBottom: '8px' }}>
              <span style={{ fontSize: '14px', fontWeight: '800', color: '#10B981' }}>🔬 VIDEO PROCESSOR &amp; QC ANALYSIS</span>
              <button onClick={() => setActiveModal(null)} style={{ background: 'transparent', border: 'none', color: '#88949E', cursor: 'pointer' }}><X size={18} /></button>
            </div>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
              <div style={{ background: '#070A0E', border: '1px solid #1E2836', borderRadius: '4px', padding: '10px' }}>
                <span style={{ fontSize: '11px', fontWeight: '700', color: '#38BDF8' }}>Waveform &amp; Vectorscope</span>
                <div style={{ height: '140px', background: '#020406', marginTop: '6px', display: 'flex', alignItems: 'center', justifyContent: 'center', color: '#4ADE80', fontFamily: 'var(--font-mono)', fontSize: '10px', border: '1px solid #122016' }}>
                  [ SMPTE ITU-R BT.709 PASS · 100 IRE ]
                </div>
              </div>
              <div style={{ background: '#070A0E', border: '1px solid #1E2836', borderRadius: '4px', padding: '10px' }}>
                <span style={{ fontSize: '11px', fontWeight: '700', color: '#38BDF8' }}>Proc Amp Adjustments</span>
                <div style={{ display: 'flex', flexDirection: 'column', gap: '8px', marginTop: '8px', fontSize: '10.5px' }}>
                  <div><span>Brightness: 0%</span><input type="range" min="-50" max="50" defaultValue="0" style={{ width: '100%', height: '4px' }} /></div>
                  <div><span>Contrast: 100%</span><input type="range" min="50" max="150" defaultValue="100" style={{ width: '100%', height: '4px' }} /></div>
                  <div><span>Saturation: 100%</span><input type="range" min="0" max="200" defaultValue="100" style={{ width: '100%', height: '4px' }} /></div>
                </div>
              </div>
            </div>
            <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: '14px' }}>
              <button onClick={() => setActiveModal(null)} style={{ padding: '6px 16px', background: '#1E293B', border: '1px solid #334155', color: '#FFF', borderRadius: '3px', cursor: 'pointer', fontSize: '11px', fontWeight: '700' }}>Close</button>
            </div>
          </div>
        </div>
      )}

      {/* MODAL 4: BROADCAST SETTINGS */}
      {activeModal === 'SETTINGS' && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.8)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 100 }}>
          <div style={{ background: '#0F151C', border: '1px solid #2B3744', borderRadius: '6px', width: '540px', padding: '18px', boxShadow: '0 0 30px rgba(0,0,0,0.9)' }}>
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '14px', borderBottom: '1px solid #222C38', paddingBottom: '8px' }}>
              <span style={{ fontSize: '14px', fontWeight: '800', color: '#FFF' }}>⚙ BROADCAST ENGINE SETTINGS</span>
              <button onClick={() => setActiveModal(null)} style={{ background: 'transparent', border: 'none', color: '#88949E', cursor: 'pointer' }}><X size={18} /></button>
            </div>
            <div style={{ display: 'flex', flexDirection: 'column', gap: '12px', fontSize: '11.5px', color: '#CBD5E1' }}>
              <div>
                <label style={{ display: 'block', color: '#88949E', marginBottom: '3px' }}>Primary SDI / NDI Output Card:</label>
                <select style={{ width: '100%', background: '#090E13', border: '1px solid #27303A', color: '#FFF', padding: '6px', borderRadius: '3px' }}>
                  <option>Blackmagic DeckLink 8K Pro (SDI 1 - 1080p50)</option>
                  <option>NDI IP Video Stream (1080p50)</option>
                  <option>Virtual DirectShow Output</option>
                </select>
              </div>
              <div>
                <label style={{ display: 'block', color: '#88949E', marginBottom: '3px' }}>Audio Engine Driver:</label>
                <select style={{ width: '100%', background: '#090E13', border: '1px solid #27303A', color: '#FFF', padding: '6px', borderRadius: '3px' }}>
                  <option>ASIO · DeckLink Audio 16-ch 24-bit 48kHz</option>
                  <option>Windows Audio WASAPI (Exclusive Low Latency)</option>
                </select>
              </div>
              <div>
                <label style={{ display: 'block', color: '#88949E', marginBottom: '3px' }}>API Gateway URL:</label>
                <input type="text" value={apiBaseUrl} onChange={e => setApiBaseUrl(e.target.value)} style={{ width: '100%', background: '#090E13', border: '1px solid #27303A', color: '#FFF', padding: '6px', borderRadius: '3px' }} />
              </div>
            </div>
            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '8px', marginTop: '18px' }}>
              <button onClick={() => setActiveModal(null)} style={{ padding: '6px 14px', background: '#1E293B', border: '1px solid #334155', color: '#FFF', borderRadius: '3px', cursor: 'pointer' }}>Cancel</button>
              <button onClick={() => { setApiBase(apiBaseUrl); setActiveModal(null); window.location.reload(); }} style={{ padding: '6px 14px', background: '#63378E', border: '1px solid #8D55B7', color: '#FFF', borderRadius: '3px', cursor: 'pointer', fontWeight: '700' }}>Save &amp; Apply</button>
            </div>
          </div>
        </div>
      )}

      {/* MODAL 5: KEYBOARD SHORTCUTS */}
      {activeModal === 'SHORTCUTS' && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.8)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 100 }}>
          <div style={{ background: '#0F151C', border: '1px solid #2B3744', borderRadius: '6px', width: '480px', padding: '16px', boxShadow: '0 0 30px rgba(0,0,0,0.9)' }}>
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '12px', borderBottom: '1px solid #222C38', paddingBottom: '8px' }}>
              <span style={{ fontSize: '14px', fontWeight: '800', color: '#FFF' }}>⌨ BROADCAST KEYBOARD SHORTCUTS</span>
              <button onClick={() => setActiveModal(null)} style={{ background: 'transparent', border: 'none', color: '#88949E', cursor: 'pointer' }}><X size={18} /></button>
            </div>
            <div style={{ display: 'flex', flexDirection: 'column', gap: '6px', fontSize: '11.5px' }}>
              {[
                { key: 'F5', action: 'TAKE selected / cued item to Program Air' },
                { key: 'F6', action: 'NEXT event in rundown' },
                { key: 'F8', action: 'STOP Program playback' },
                { key: 'Space', action: 'Play / Pause Program playback' },
                { key: 'F11', action: 'Toggle Fullscreen Confidence Monitor' },
                { key: 'Ctrl+N', action: 'New blank playlist' },
                { key: 'Ctrl+O', action: 'Open / import playlist file' },
                { key: 'Ctrl+S', action: 'Save current playlist' }
              ].map((sc, i) => (
                <div key={i} style={{ display: 'flex', justifyContent: 'space-between', padding: '5px 8px', background: i % 2 === 0 ? '#0B1015' : 'transparent', borderRadius: '2px' }}>
                  <span style={{ fontWeight: '700', color: '#38BDF8', fontFamily: 'var(--font-mono)' }}>{sc.key}</span>
                  <span style={{ color: '#C8D2DA' }}>{sc.action}</span>
                </div>
              ))}
            </div>
            <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: '14px' }}>
              <button onClick={() => setActiveModal(null)} style={{ padding: '6px 14px', background: '#1E293B', border: '1px solid #334155', color: '#FFF', borderRadius: '3px', cursor: 'pointer', fontSize: '11px', fontWeight: '700' }}>Close</button>
            </div>
          </div>
        </div>
      )}

      {/* MODAL 6: ABOUT */}
      {activeModal === 'ABOUT' && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.8)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 100 }}>
          <div style={{ background: '#0F151C', border: '1px solid #2B3744', borderRadius: '6px', width: '420px', padding: '20px', textAlign: 'center', boxShadow: '0 0 30px rgba(0,0,0,0.9)' }}>
            <Tv size={36} color="#B78BE7" style={{ margin: '0 auto 10px' }} />
            <h3 style={{ fontSize: '16px', fontWeight: '800', color: '#FFF' }}>Kashtrix Playout Web</h3>
            <div style={{ fontSize: '11px', color: '#38BDF8', marginTop: '2px' }}>Master Control Automation &amp; Channel Controller</div>
            <div style={{ fontSize: '11px', color: '#88949E', marginTop: '12px', lineHeight: 1.6 }}>
              Enterprise Playout Automation Suite<br />
              DVB / SDI / NDI / SCTE-35 / MOS 2.8.4 Protocol Compliant<br />
              Build: 2026.10-FIX49H · Powered by Kashtrix Core
            </div>
            <button onClick={() => setActiveModal(null)} style={{ marginTop: '18px', padding: '6px 20px', background: '#63378E', border: '1px solid #8D55B7', color: '#FFF', borderRadius: '3px', cursor: 'pointer', fontWeight: '700', fontSize: '11px' }}>OK</button>
          </div>
        </div>
      )}

      {/* MODAL 7: EXPORT EPG */}
      {activeModal === 'EXPORT_EPG' && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.8)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 100 }}>
          <div style={{ background: '#0F151C', border: '1px solid #2B3744', borderRadius: '6px', width: '560px', padding: '16px', boxShadow: '0 0 30px rgba(0,0,0,0.9)' }}>
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '12px', borderBottom: '1px solid #222C38', paddingBottom: '8px' }}>
              <span style={{ fontSize: '14px', fontWeight: '800', color: '#38BDF8' }}>📤 EXPORT EPG (ELECTRONIC PROGRAM GUIDE)</span>
              <button onClick={() => setActiveModal(null)} style={{ background: 'transparent', border: 'none', color: '#88949E', cursor: 'pointer' }}><X size={18} /></button>
            </div>
            <p style={{ fontSize: '11px', color: '#9AA4AB', marginBottom: '10px' }}>
              Export current broadcast schedule to standard XMLTV or JSON schema for DVB transmission, IPTV middleware, or web guides.
            </p>
            <div style={{ background: '#090E13', border: '1px solid #1E2836', borderRadius: '4px', padding: '10px', height: '140px', overflowY: 'auto', fontFamily: 'var(--font-mono)', fontSize: '10px', color: '#4ADE80' }}>
              {`<?xml version="1.0" encoding="utf-8"?>
<tv generator-info-name="Kashtrix Playout">
  <channel id="${activeChannelId}">
    <display-name>${channelStatus?.channelName || activeChannelId}</display-name>
  </channel>
${playlist.map((p, idx) => `  <programme start="${p.airTime}" stop="${p.outPoint}" channel="${activeChannelId}">
    <title>${p.title}</title>
    <category>${p.category}</category>
  </programme>`).join('\n')}
</tv>`}
            </div>
            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '8px', marginTop: '14px' }}>
              <button onClick={() => { addLog('Exported XMLTV EPG to downloads.', 'SUCCESS'); setActiveModal(null); }} style={{ padding: '6px 14px', background: '#63378E', border: '1px solid #8D55B7', color: '#FFF', borderRadius: '3px', cursor: 'pointer', fontWeight: '700', fontSize: '11px', display: 'flex', alignItems: 'center', gap: '5px' }}>
                <Download size={13} /> Export XMLTV
              </button>
            </div>
          </div>
        </div>
      )}

      {/* MODAL 8: GATEWAY CONFIG MODAL */}
      {activeModal === 'CONFIG' && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.8)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 100 }}>
          <div style={{ background: '#0F151C', border: '1px solid #2B3744', borderRadius: '6px', width: '420px', padding: '18px', boxShadow: '0 0 30px rgba(0,0,0,0.9)' }}>
            <h3 style={{ fontSize: '14px', fontWeight: '800', marginBottom: '10px', color: '#FFF' }}>Kashtrix Playout Gateway Connection</h3>
            <label style={{ fontSize: '11px', color: '#88949E', display: 'block', marginBottom: '4px' }}>API Base URL:</label>
            <input type="text" value={apiBaseUrl} onChange={e => setApiBaseUrl(e.target.value)} style={{ width: '100%', background: '#080C10', border: '1px solid #27303A', color: '#FFF', padding: '6px 10px', borderRadius: '3px', fontSize: '12px', marginBottom: '14px' }} />
            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '8px' }}>
              <button onClick={() => setActiveModal(null)} style={{ padding: '6px 12px', background: '#1A232E', border: '1px solid #2B3744', color: '#C8D2DA', borderRadius: '3px', cursor: 'pointer', fontSize: '11px' }}>Cancel</button>
              <button onClick={() => { setApiBase(apiBaseUrl); setActiveModal(null); window.location.reload(); }} style={{ padding: '6px 14px', background: '#63378E', border: '1px solid #8D55B7', color: '#FFF', borderRadius: '3px', cursor: 'pointer', fontWeight: '700', fontSize: '11px' }}>Save &amp; Connect</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
