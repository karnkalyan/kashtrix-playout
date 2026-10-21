'use client';

import React, { useState, useEffect } from 'react';
import Link from 'next/link';
import { motion, AnimatePresence } from 'framer-motion';
import { 
  Tv, 
  Video, 
  Layers, 
  LayoutGrid, 
  ArrowRight, 
  Sparkles, 
  Radio, 
  Activity, 
  Cpu, 
  ShieldCheck, 
  Play, 
  Database,
  ArrowUpRight,
  Volume2,
  FileText,
  CreditCard,
  CheckCircle2,
  ExternalLink
} from 'lucide-react';
import { ScreenshotCard } from './ScreenshotModal';

interface HeroProps {
  content?: {
    badge: string;
    title: string;
    gradientTitle: string;
    subtitle: string;
    primaryCta: string;
    primaryCtaUrl: string;
    secondaryCta: string;
    secondaryCtaUrl: string;
    stats: string;
  };
}

const PREVIEW_TABS = [
  {
    id: 'playout',
    slug: 'playout',
    label: 'Playout Engine',
    badge: 'PROGRAM / PREVIEW',
    icon: Tv,
    image: '/assets/products/playout-master-rundown.png',
    title: 'Kashtrix Playout 24/7 Master Transmission Surface',
    caption: 'Hardware baseband SDI, ST 2110 IP output, automated commercial break insertion & EBU R128 VU metering.'
  },
  {
    id: 'studio',
    slug: 'studio',
    label: 'Studio Switcher',
    badge: '12-CH MULTIVIEW',
    icon: Video,
    image: '/assets/products/studio-switcher-full.png',
    title: 'Kashtrix Studio Live Production Switcher',
    caption: '12-channel multiview, audio EQ mixing desk, DDR playback servers & hardware DVE transitions.'
  },
  {
    id: 'cg',
    slug: 'cg-editor',
    label: '3D CG Editor',
    badge: 'MOTION GRAPHICS',
    icon: Layers,
    image: '/assets/products/cg-editor-main.png',
    title: 'Kashtrix CG Editor 3D Motion Graphics Compositor',
    caption: 'Multi-track keyframe timelines, bezier velocity curves & real-time JSON/MOS dynamic field bindings.'
  },
  {
    id: 'nrcs-web',
    slug: 'nrcs-web',
    label: 'Kashtrix NRCS Web',
    badge: 'CLOUD NEWSROOM',
    icon: FileText,
    image: '/assets/products/nrcs-newsroom-workflow.png',
    title: 'Kashtrix NRCS Web Cloud Newsroom Computer System',
    caption: 'Browser story authoring, MOS 2.8.4 plugins, live prompter push & automated bulletin rundown timing.'
  },
  {
    id: 'streamops',
    slug: 'streamops',
    label: 'StreamOps OTT',
    badge: 'MULTI-CDN OTT',
    icon: Radio,
    image: '/assets/products/pro-io-st2110-scte.png',
    title: 'StreamOps Cloud & Hybrid Transcoder & Distribution',
    caption: 'Low-latency HLS/DASH, hardware NVENC acceleration, SCTE-35 digital ad signaling & multi-CDN failover.'
  },
  {
    id: 'oss-bss',
    slug: 'oss-bss',
    label: 'Kashtrix OSS / BSS',
    badge: 'OPERATIONS & BILLING',
    icon: CreditCard,
    image: '/assets/products/channel-controller-cluster.png',
    title: 'Kashtrix OSS / BSS Broadcast & Telecom Operations',
    caption: 'Airtime contract reconciliation, automated as-run billing, subscriber lifecycle & channel provisioning.'
  },
  {
    id: 'mam',
    slug: 'mam',
    label: 'Enterprise MAM',
    badge: 'MEDIA VAULT',
    icon: Database,
    image: '/assets/products/mam-catalog.png',
    title: 'Enterprise Media Asset Management & Catalog',
    caption: 'Automated metadata extraction, proxy generation, and high-speed multi-track SAN/NAS media indexing.'
  },
  {
    id: 'multiview',
    slug: 'multiview',
    label: 'Multiview & QC',
    badge: 'CONFIDENCE WALL',
    icon: LayoutGrid,
    image: '/assets/products/multiview-wall-qc.png',
    title: 'Broadcast Multi-Source Confidence Monitor Wall',
    caption: 'Zero-drop multi-camera monitoring, freeze/black alarms, and multi-channel audio phase graphs.'
  }
];

export function BroadcastHero({ content }: HeroProps) {
  const [activeTab, setActiveTab] = useState(PREVIEW_TABS[0]);
  const [timecode, setTimecode] = useState('14:02:49:18');

  // Real-time ticking broadcast timecode
  useEffect(() => {
    const timer = setInterval(() => {
      const now = new Date();
      const hh = String(now.getHours()).padStart(2, '0');
      const mm = String(now.getMinutes()).padStart(2, '0');
      const ss = String(now.getSeconds()).padStart(2, '0');
      const ff = String(Math.floor((now.getMilliseconds() / 1000) * 50)).padStart(2, '0');
      setTimecode(`${hh}:${mm}:${ss}:${ff}`);
    }, 40);
    return () => clearInterval(timer);
  }, []);

  const stats = content?.stats
    ? JSON.parse(content.stats)
    : [
        { value: '99.999%', label: 'On-Air Availability', description: 'Dual 1+1 Mirror & N+M failover redundancy' },
        { value: '< 1 Frame', label: 'Switching Latency', description: 'Zero-drop hardware baseband SDI & NDI 6' },
        { value: '4K / UHD / HDR', label: 'Format Support', description: 'Full 1080i50, 1080p60, 2160p50/60 pipelines' },
        { value: '17 Modules', label: 'Ecosystem Breadth', description: 'Playout, CG, MAM, NRCS, StreamOps, OSS/BSS' }
      ];

  return (
    <section className="relative pt-32 pb-20 overflow-hidden bg-[var(--bg-page)] transition-colors duration-200">
      {/* Dynamic Ambient Glows */}
      <div className="absolute top-1/4 left-1/2 -translate-x-1/2 w-[1000px] h-[500px] bg-[var(--accent)]/10 rounded-full blur-[140px] pointer-events-none" />
      <div className="absolute top-16 left-12 w-80 h-80 bg-[var(--accent)]/5 rounded-full blur-[100px] pointer-events-none" />

      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 relative z-10">
        
        {/* On-Air Live Operational Badge */}
        <motion.div
          initial={{ opacity: 0, y: -15 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.5 }}
          className="flex flex-wrap items-center justify-center gap-3 mb-6"
        >
          <div className="inline-flex items-center gap-2.5 px-4 py-1.5 rounded-full bg-[var(--bg-card)] border border-[var(--border-subtle)] shadow-sm text-xs font-semibold text-[var(--text-main)]">
            <span className="relative flex h-2.5 w-2.5">
              <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-80" />
              <span className="relative inline-flex rounded-full h-2.5 w-2.5 bg-emerald-500" />
            </span>
            <span className="tracking-wider uppercase text-[11px] font-black text-emerald-600 dark:text-emerald-400">
              OPERATIONAL BROADCAST SUITE
            </span>
            <span className="text-[var(--border-strong)]">•</span>
            <span className="text-[var(--text-muted)] font-mono text-[11px]">
              {content?.badge || 'ENTERPRISE BROADCAST ARCHITECTURE'}
            </span>
          </div>

          <div className="hidden sm:inline-flex items-center gap-2 px-3.5 py-1 rounded-full bg-emerald-500/10 border border-emerald-500/25 text-[11px] font-mono text-emerald-700 dark:text-emerald-400">
            <Radio className="w-3.5 h-3.5" />
            <span>SMPTE ST 2110 / SDI 12G / NDI 6 LOCKED</span>
          </div>
        </motion.div>

        {/* Central Headlines */}
        <div className="text-center max-w-4xl mx-auto mb-10">
          <motion.h1
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.6, delay: 0.1 }}
            className="text-4xl sm:text-5xl lg:text-6xl font-black tracking-tight leading-[1.08] text-[var(--text-main)]"
          >
            {content?.title || 'Enterprise TV Playout & Live Production'}{' '}
            <span className="text-[var(--accent)] block mt-1.5 drop-shadow-xs">
              {content?.gradientTitle || 'Unified in Kashtrix'}
            </span>
          </motion.h1>

          <motion.p
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.6, delay: 0.2 }}
            className="mt-5 text-sm sm:text-base text-[var(--text-secondary)] max-w-2xl mx-auto leading-relaxed"
          >
            {content?.subtitle ||
              'A complete software broadcast ecosystem engineered for 24/7 mission-critical television channels, newsrooms, sports production, and streaming networks. Built with baseband SDI, ST 2110, NDI 6, real-time 3D motion graphics, MAM, StreamOps OTT, and Kashtrix OSS/BSS.'}
          </motion.p>

          {/* Action CTAs */}
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.6, delay: 0.3 }}
            className="mt-7 flex flex-wrap items-center justify-center gap-3.5"
          >
            <Link
              href={content?.secondaryCtaUrl || '/contact'}
              className="px-6 py-3 rounded-xl text-xs sm:text-sm font-bold text-white bg-[var(--accent)] hover:bg-[var(--accent-dark)] hover:shadow-lg hover:shadow-[var(--accent)]/25 transition-all duration-300 flex items-center gap-2 cursor-pointer"
            >
              <Sparkles className="w-4 h-4 text-white" />
              <span>{content?.secondaryCta || 'Request Engineering Demo'}</span>
            </Link>

            <Link
              href={content?.primaryCtaUrl || '/products'}
              className="px-6 py-3 rounded-xl text-xs sm:text-sm font-bold text-[var(--text-main)] bg-[var(--bg-card)] hover:bg-[var(--bg-card-hover)] border border-[var(--border-strong)] hover:border-[var(--accent)] transition-all duration-300 flex items-center gap-2 cursor-pointer shadow-xs"
            >
              <span>{content?.primaryCta || 'Explore All 17 Software Products'}</span>
              <ArrowRight className="w-4 h-4 text-[var(--text-muted)]" />
            </Link>

            <Link
              href="/admin"
              className="px-4 py-3 rounded-xl text-xs font-semibold text-[var(--text-secondary)] hover:text-[var(--text-main)] bg-[var(--bg-surface)] border border-[var(--border-subtle)] hover:border-[var(--accent)] transition-all flex items-center gap-1.5 cursor-pointer shadow-xs"
            >
              <span className="w-2 h-2 rounded-full bg-emerald-500" />
              <span>MCR CMS Admin</span>
              <ArrowUpRight className="w-3.5 h-3.5 text-[var(--text-muted)]" />
            </Link>
          </motion.div>
        </div>

        {/* Interactive Floating Broadcast Workstation Console */}
        <motion.div
          initial={{ opacity: 0, scale: 0.98, y: 25 }}
          animate={{ opacity: 1, scale: 1, y: 0 }}
          transition={{ duration: 0.7, delay: 0.4 }}
          className="mt-4 max-w-5xl mx-auto relative"
        >
          {/* Software Workstation Tabs Switcher */}
          <div className="flex flex-wrap items-center justify-center gap-1.5 mb-3 p-1 rounded-2xl bg-[var(--bg-card)] border border-[var(--border-subtle)] max-w-4xl mx-auto shadow-sm">
            {PREVIEW_TABS.map((tab) => {
              const IconCmp = tab.icon;
              const isActive = activeTab.id === tab.id;
              return (
                <button
                  key={tab.id}
                  onClick={() => setActiveTab(tab)}
                  className={`relative flex items-center gap-1.5 px-3 py-1.5 rounded-xl text-xs font-medium transition-all cursor-pointer ${
                    isActive
                      ? 'text-white'
                      : 'text-[var(--text-muted)] hover:text-[var(--text-main)] hover:bg-[var(--bg-card-hover)]'
                  }`}
                >
                  {isActive && (
                    <motion.div
                      layoutId="activeTabPill"
                      className="absolute inset-0 rounded-xl bg-[var(--accent)] shadow-sm"
                      transition={{ type: 'spring', duration: 0.35 }}
                    />
                  )}
                  <IconCmp className="w-3.5 h-3.5 relative z-10" />
                  <span className="relative z-10 font-semibold">{tab.label}</span>
                </button>
              );
            })}
          </div>

          {/* Master Workstation Frame with Hardware Chrome Controls */}
          <div className="relative rounded-2xl p-2.5 bg-[var(--bg-card)] border border-[var(--border-subtle)] shadow-xl">
            {/* Console Chrome Header with VU Meters & Running Timecode */}
            <div className="flex items-center justify-between px-4 py-2.5 bg-[var(--bg-surface)] rounded-t-xl border-b border-[var(--border-subtle)]">
              <div className="flex items-center gap-2">
                <span className="w-2.5 h-2.5 rounded-full bg-[#EF4444] inline-block" />
                <span className="w-2.5 h-2.5 rounded-full bg-[#F59E0B] inline-block" />
                <span className="w-2.5 h-2.5 rounded-full bg-[#10B981] inline-block" />
                <span className="ml-2 text-xs font-mono font-bold text-[var(--text-main)] truncate max-w-xs sm:max-w-md">
                  KASHTRIX SUITE • {activeTab.title}
                </span>
              </div>

              {/* Live Timecode & VU Meter Simulation Bar */}
              <div className="hidden md:flex items-center gap-4">
                {/* Timecode display */}
                <div className="px-2 py-0.5 rounded bg-black/80 border border-slate-700 font-mono text-[11px] font-bold text-amber-400">
                  <span className="text-[9px] text-slate-400 mr-1 uppercase">TC</span>
                  {timecode}
                </div>

                {/* Animated Stereo VU Meters */}
                <div className="flex items-end gap-1.5 h-5 px-1.5 py-0.5 rounded bg-black/80 border border-slate-700">
                  {/* Left Channel */}
                  <div className="flex items-end gap-0.5 h-full">
                    <div className="w-1 bg-[#10B981] rounded-xs h-2.5 animate-pulse" />
                    <div className="w-1 bg-[#10B981] rounded-xs h-3.5 animate-bounce" />
                    <div className="w-1 bg-[#F59E0B] rounded-xs h-1.5 animate-pulse" />
                    <div className="w-1 bg-[#EF4444] rounded-xs h-2 animate-bounce" />
                  </div>
                  {/* Right Channel */}
                  <div className="flex items-end gap-0.5 h-full ml-1">
                    <div className="w-1 bg-[#10B981] rounded-xs h-3.5 animate-bounce" />
                    <div className="w-1 bg-[#10B981] rounded-xs h-2.5 animate-pulse" />
                    <div className="w-1 bg-[#F59E0B] rounded-xs h-2 animate-bounce" />
                    <div className="w-1 bg-[#EF4444] rounded-xs h-1.5 animate-pulse" />
                  </div>
                </div>

                <div className="text-[10px] font-mono text-emerald-600 dark:text-emerald-400 font-bold flex items-center gap-1.5">
                  <span className="w-1.5 h-1.5 rounded-full bg-emerald-500 animate-ping" />
                  <span>50.00 FPS • DIRECTX 11</span>
                </div>
              </div>
            </div>

            {/* Screenshot Display */}
            <AnimatePresence mode="wait">
              <motion.div
                key={activeTab.id}
                initial={{ opacity: 0, scale: 0.99 }}
                animate={{ opacity: 1, scale: 1 }}
                exit={{ opacity: 0, scale: 0.99 }}
                transition={{ duration: 0.25 }}
              >
                <ScreenshotCard
                  src={activeTab.image}
                  alt={activeTab.title}
                  title={activeTab.title}
                  caption={activeTab.caption}
                  className="rounded-none border-x-0 border-t-0"
                />
              </motion.div>
            </AnimatePresence>

            {/* Workstation Deep-Dive Footer Bar */}
            <div className="flex flex-col sm:flex-row items-center justify-between gap-3 px-4 py-3 bg-[var(--bg-surface)] rounded-b-xl border-t border-[var(--border-subtle)]">
              <div className="flex items-center gap-2 text-xs text-[var(--text-secondary)] font-medium">
                <span className="w-2 h-2 rounded-full bg-emerald-500 inline-block shrink-0" />
                <span className="truncate max-w-lg">{activeTab.caption}</span>
              </div>
              <Link
                href={`/products/${activeTab.slug}`}
                className="inline-flex items-center gap-1.5 px-3.5 py-1.5 rounded-lg text-xs font-bold text-white bg-[var(--accent)] hover:bg-[var(--accent-dark)] transition-all shadow-xs group shrink-0"
              >
                <span>Open Dedicated {activeTab.label} Page & Specs</span>
                <ArrowRight className="w-3.5 h-3.5 group-hover:translate-x-0.5 transition-transform" />
              </Link>
            </div>
          </div>
        </motion.div>

        {/* 4-Metric Statistics Bar */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.6, delay: 0.5 }}
          className="mt-12 grid grid-cols-2 md:grid-cols-4 gap-4 max-w-5xl mx-auto"
        >
          {stats.map((stat: { value: string; label: string; description: string }, idx: number) => (
            <div
              key={idx}
              className="p-4 rounded-xl bg-[var(--bg-card)] border border-[var(--border-subtle)] hover:border-[var(--border-strong)] transition-all shadow-xs"
            >
              <div className="text-xl sm:text-2xl font-black text-[var(--text-main)]">
                {stat.value}
              </div>
              <div className="text-[11px] font-bold text-[var(--accent)] uppercase tracking-wider mt-0.5">
                {stat.label}
              </div>
              <p className="text-[11px] text-[var(--text-muted)] mt-1 line-clamp-2 leading-relaxed">
                {stat.description}
              </p>
            </div>
          ))}
        </motion.div>

      </div>
    </section>
  );
}
