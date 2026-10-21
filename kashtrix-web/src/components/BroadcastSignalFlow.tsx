'use client';

import React, { useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { 
  Radio, 
  Database, 
  FileText, 
  Tv, 
  Layers, 
  Server, 
  Activity, 
  CheckCircle2, 
  ArrowRight,
  Zap,
  Cpu,
  ShieldCheck,
  CreditCard
} from 'lucide-react';

const PIPELINE_STAGES = [
  {
    step: '01',
    id: 'ingest',
    title: 'Ingest Capture',
    subtitle: 'Multi-Source Acquisition',
    desc: 'Baseband SDI, NDI 6 & low-latency SRT ingest server. Real-time proxy transcoding and automated SHA-256 integrity check.',
    icon: Radio,
    color: '#D97706',
    specs: {
      input: '12G-SDI / NDI 6 / SRT',
      codec: 'Apple ProRes 422 HQ / DNxHD',
      latency: '< 1 Frame Direct Stream',
      monitoring: 'Confidence Grid Active'
    }
  },
  {
    step: '02',
    id: 'mam',
    title: 'Enterprise MAM',
    subtitle: 'Central Asset Catalog',
    desc: 'Enterprise media asset repository with permanent GUID tagging, frame-accurate scrub timeline, and sub-second clip retrieval.',
    icon: Database,
    color: '#059669',
    specs: {
      storage: 'High-Speed SAN / NVMe Pool',
      metadata: 'MOS XML & JSON Dynamic Tags',
      validation: 'EBU R128 Loudness Normalization',
      checksum: 'SHA-256 Bit-Exact Verifier'
    }
  },
  {
    step: '03',
    id: 'nrcs',
    title: 'Kashtrix NRCS Web',
    subtitle: 'Cloud Newsroom Rundowns',
    desc: 'Browser-native newsroom system with MOS 2.8.4 HTML5 plugin. Synchronizes live reporter copy, MAM clips, and studio teleprompters in real time.',
    icon: FileText,
    color: '#7C3AED',
    specs: {
      protocol: 'MOS 2.8.4 HTML5 ActiveX Bus',
      prompter: 'Dual Screen Smooth GPU Scroll',
      sync: 'Zero-Lag Story Push to Playout',
      collaboration: 'Multi-User Concurrent Lock'
    }
  },
  {
    step: '04',
    id: 'cg',
    title: '3D Motion Graphics',
    subtitle: 'Real-Time Compositor',
    desc: 'Real-time 3D motion graphics engine with bezier curve keyframe animation, multi-track timeline, and automated dynamic data overlays.',
    icon: Layers,
    color: '#DB2777',
    specs: {
      render: 'DirectX 11 Hardware Compositor',
      alpha: 'Linear Key / Fill 32-bit Alpha',
      dataBind: 'Real-Time RSS / JSON / Stocks',
      lowerThirds: 'Automated Rundown Sync'
    }
  },
  {
    step: '05',
    id: 'playout',
    title: 'Master Playout',
    subtitle: '24/7 Transmission Engine',
    desc: 'Mission-critical broadcast playout engine with Program/Preview buses, frame-accurate trimming, commercial break insertion, and live bug overlays.',
    icon: Tv,
    color: '#6D28D9',
    specs: {
      timing: 'PTP IEEE 1588 Genlock Sync',
      breakInsertion: 'SCTE-35 / SCTE-104 Ad Digital Cues',
      buses: 'Independent PVW and PGM',
      audio: '16-Channel Embedded PCM + Dolby'
    }
  },
  {
    step: '06',
    id: 'streamops',
    title: 'StreamOps OTT',
    subtitle: 'Multi-CDN Live Transcoder',
    desc: 'High-density multi-profile live video transcoder. Delivers Low-Latency HLS, MPEG-DASH, and SRT with hardware NVENC acceleration and SCTE-35 SSAI.',
    icon: Server,
    color: '#0284C7',
    specs: {
      profiles: '4K UHD, 1080p60, 720p ABR Ladder',
      latency: '< 3s Glass-to-Glass LL-HLS',
      adSignaling: 'SCTE-35 Splice Cue Passthrough',
      routing: 'Multi-CDN Failover Routing'
    }
  },
  {
    step: '07',
    id: 'oss-bss',
    title: 'Kashtrix OSS / BSS',
    subtitle: 'Reconciliation & Billing',
    desc: 'Reconciles Playout as-run transmission logs against advertiser contracts, generates verified PDF invoices, and manages subscriber packages.',
    icon: CreditCard,
    color: '#7C3AED',
    specs: {
      trafficSync: 'Playout As-Run Ingestion',
      invoicing: 'Automated Sponsor Billing',
      subscribers: 'Tiered Channel Entitlements',
      apiIntegration: 'REST & GraphQL Gateways'
    }
  }
];

export function BroadcastSignalFlow() {
  const [activeStage, setActiveStage] = useState(PIPELINE_STAGES[4]); // Default to Playout

  return (
    <div className="space-y-8">
      {/* Interactive Signal Flow Stages Grid */}
      <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-7 gap-2.5">
        {PIPELINE_STAGES.map((stage) => {
          const IconComponent = stage.icon;
          const isActive = activeStage.id === stage.id;
          return (
            <button
              key={stage.id}
              onClick={() => setActiveStage(stage)}
              className={`p-3.5 rounded-2xl text-left transition-all duration-200 relative group cursor-pointer border ${
                isActive
                  ? 'bg-[var(--bg-card)] border-[var(--accent)] shadow-md -translate-y-0.5'
                  : 'bg-[var(--bg-surface)] border-[var(--border-subtle)] hover:border-[var(--border-strong)] hover:bg-[var(--bg-card-hover)]'
              }`}
            >
              {/* Active top line indicator */}
              {isActive && (
                <div className="absolute inset-x-0 top-0 h-1 bg-[var(--accent)] rounded-t-2xl" />
              )}

              <div className="flex items-center justify-between mb-2">
                <span className={`text-[10px] font-mono font-bold ${isActive ? 'text-[var(--accent)]' : 'text-[var(--text-muted)]'}`}>
                  {stage.step}
                </span>
                <div
                  className="w-7 h-7 rounded-lg flex items-center justify-center transition-transform group-hover:scale-105"
                  style={{
                    backgroundColor: `${stage.color}15`,
                    color: stage.color,
                    border: `1px solid ${stage.color}30`
                  }}
                >
                  <IconComponent className="w-3.5 h-3.5" />
                </div>
              </div>

              <div className="font-bold text-xs text-[var(--text-main)] group-hover:text-[var(--accent)] transition-colors truncate">
                {stage.title}
              </div>
              <div className="text-[10px] text-[var(--text-muted)] font-mono mt-0.5 truncate">
                {stage.subtitle}
              </div>

              {/* Status Dot */}
              <div className="mt-2.5 flex items-center gap-1.5 text-[9px] font-mono text-[var(--text-muted)]">
                <span
                  className="w-1.5 h-1.5 rounded-full animate-pulse"
                  style={{ backgroundColor: stage.color }}
                />
                <span>READY</span>
              </div>
            </button>
          );
        })}
      </div>

      {/* Active Stage Technical Deep-Dive Panel */}
      <AnimatePresence mode="wait">
        <motion.div
          key={activeStage.id}
          initial={{ opacity: 0, y: 10 }}
          animate={{ opacity: 1, y: 0 }}
          exit={{ opacity: 0, y: -10 }}
          transition={{ duration: 0.2 }}
          className="rounded-2xl p-6 sm:p-7 bg-[var(--bg-card)] border border-[var(--border-subtle)] shadow-md relative overflow-hidden"
        >
          <div className="grid grid-cols-1 lg:grid-cols-12 gap-8 items-center relative z-10">
            {/* Left description */}
            <div className="lg:col-span-7 space-y-3.5">
              <div className="flex items-center gap-2">
                <span className="px-2 py-0.5 rounded text-[10px] font-mono font-bold tracking-wider uppercase text-white"
                  style={{ backgroundColor: activeStage.color }}
                >
                  PIPELINE STAGE {activeStage.step}
                </span>
                <span className="text-xs font-mono text-[var(--text-muted)]">
                  AUTOMATED BUS ROUTE
                </span>
              </div>

              <h3 className="text-xl sm:text-2xl font-black text-[var(--text-main)] tracking-tight">
                {activeStage.title}: {activeStage.subtitle}
              </h3>

              <p className="text-xs sm:text-sm text-[var(--text-secondary)] leading-relaxed">
                {activeStage.desc}
              </p>

              <div className="pt-1 flex flex-wrap items-center gap-2">
                <div className="px-2.5 py-1 rounded-lg bg-[var(--bg-surface)] border border-[var(--border-subtle)] text-[11px] font-mono text-emerald-600 dark:text-emerald-400 flex items-center gap-1.5">
                  <CheckCircle2 className="w-3.5 h-3.5" />
                  <span>Bit-Exact 10-bit Synchronization</span>
                </div>
                <div className="px-2.5 py-1 rounded-lg bg-[var(--bg-surface)] border border-[var(--border-subtle)] text-[11px] font-mono text-[var(--text-muted)] flex items-center gap-1.5">
                  <Cpu className="w-3.5 h-3.5 text-[var(--accent)]" />
                  <span>DirectX 11 GPU Pipeline</span>
                </div>
              </div>
            </div>

            {/* Right specs table */}
            <div className="lg:col-span-5 bg-[var(--bg-surface)] rounded-xl p-4 border border-[var(--border-subtle)] space-y-2 font-mono text-xs">
              <div className="text-[10px] font-bold text-[var(--text-muted)] uppercase tracking-wider pb-1.5 border-b border-[var(--border-subtle)] flex items-center justify-between">
                <span>Signal Parameters</span>
                <span className="text-emerald-600 dark:text-emerald-400 font-semibold">● GENLOCK LOCKED</span>
              </div>

              {Object.entries(activeStage.specs).map(([key, val], idx) => (
                <div key={idx} className="flex justify-between py-1 border-b border-[var(--border-subtle)] last:border-0 text-[11px]">
                  <span className="text-[var(--text-muted)] capitalize">{key}:</span>
                  <span className="text-[var(--text-main)] font-semibold text-right">{val}</span>
                </div>
              ))}
            </div>
          </div>
        </motion.div>
      </AnimatePresence>
    </div>
  );
}
