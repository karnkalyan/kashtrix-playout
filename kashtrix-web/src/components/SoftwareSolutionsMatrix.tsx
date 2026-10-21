'use client';

import React, { useState } from 'react';
import Link from 'next/link';
import { motion, AnimatePresence } from 'framer-motion';
import { 
  Tv, 
  Video, 
  Layers, 
  Radio, 
  CreditCard, 
  FileText, 
  ArrowRight, 
  CheckCircle2, 
  Sparkles, 
  ShieldCheck, 
  Database, 
  Network, 
  Calendar,
  LayoutGrid,
  ExternalLink
} from 'lucide-react';
import { ScreenshotCard } from './ScreenshotModal';

interface DomainModule {
  name: string;
  slug: string;
  badge: string;
  desc: string;
  icon: React.ElementType;
}

interface WorkflowDomain {
  id: string;
  label: string;
  category: string;
  headline: string;
  tagline: string;
  description: string;
  image: string;
  standards: string[];
  modules: DomainModule[];
  keyCapabilities: string[];
}

const WORKFLOW_DOMAINS: WorkflowDomain[] = [
  {
    id: 'master-control',
    label: '24/7 Master Control Playout',
    category: 'Linear Automation',
    headline: 'Continuous On-Air Master Control & Channel Automation',
    tagline: 'Dual Preview/Program buses with sub-frame switching and SCTE-35 digital cue triggers',
    description:
      'Engineered for 24/7 mission-critical linear television broadcast. Kashtrix orchestrates gapless playlist playback, dynamic secondary graphics triggering, automated commercial insertion, EBU R128 loudness normalization, and as-run traffic reconciliation across baseband SDI, ST 2110, and NDI 6.',
    image: '/assets/products/playout-master-rundown.png',
    standards: ['SMPTE ST 2110', '12G-SDI Baseband', 'NDI 6 HDR', 'SCTE-35 / 104', 'EBU R128', 'BXF Traffic'],
    modules: [
      {
        name: 'Kashtrix Playout Engine',
        slug: 'playout',
        badge: 'FLAGSHIP',
        desc: 'DirectX 11 hardware-accelerated playback with Program/Preview buses',
        icon: Tv
      },
      {
        name: 'Master Channel Controller',
        slug: 'channel-controller',
        badge: 'SUPERVISOR',
        desc: 'Multi-station orchestration grid supervising remote playout nodes',
        icon: Network
      },
      {
        name: 'HA 1+1 Redundancy Mirror',
        slug: 'ha-redundancy',
        badge: 'ZERO-DOWNTIME',
        desc: 'Active/backup lockstep synchronization with sub-second failover',
        icon: ShieldCheck
      },
      {
        name: 'Scheduler & Traffic Master',
        slug: 'scheduler',
        badge: 'BXF 24/7',
        desc: 'Linear day timeline, commercial break blocks & as-run reconciliation',
        icon: Calendar
      }
    ],
    keyCapabilities: [
      'Sub-frame cutover and auto-dissolve transitions with zero dropped frames',
      'Frame-accurate SCTE-35 splice cue insertion for automated downstream ad insertion',
      'Automated secondary graphics triggering keyed to program metadata',
      'Continuous 1+1 active mirror failover with automated hardware bypass'
    ]
  },
  {
    id: 'newsroom',
    label: 'Newsroom & MOS Journalism',
    category: 'Editorial & Rundown',
    headline: 'Unified Browser & Desktop Newsroom Computer System',
    tagline: 'Live story authoring, MOS 2.8.4 media binding, and instant studio prompter sync',
    description:
      'Kashtrix NRCS Web untethers newsroom teams from legacy hardware terminals. Reporters and producers write stories, link approved video from MAM, attach 3D CG lower-thirds with live previews, and push real-time rundown reorderings to Playout and Studio Teleprompters simultaneously.',
    image: '/assets/products/nrcs-newsroom-workflow.png',
    standards: ['MOS 2.8.4', 'HTML5 ActiveX Plugin', 'SMPTE 2021', 'WebSocket Sync', 'Unicode UTF-8'],
    modules: [
      {
        name: 'Kashtrix NRCS Web',
        slug: 'nrcs-web',
        badge: 'CLOUD NEWSROOM',
        desc: 'Browser-native story authoring with live word-count timing',
        icon: FileText
      },
      {
        name: 'Kashtrix NRCS Core',
        slug: 'nrcs',
        badge: 'DESKTOP MOS',
        desc: 'Enterprise newsroom server with AP ENPS / Avid iNEWS interoperability',
        icon: FileText
      },
      {
        name: 'Broadcast Teleprompter',
        slug: 'prompter',
        badge: 'STUDIO PROMPTER',
        desc: 'GPU-accelerated talent display auto-following rundown revisions',
        icon: Tv
      },
      {
        name: 'CG On-Air Controller',
        slug: 'cg-controller',
        badge: 'LOWER-THIRDS',
        desc: 'Quick-edit lower thirds and breaking news tickers driven by journalists',
        icon: Sparkles
      }
    ],
    keyCapabilities: [
      'Cloud & local news scripting with real-time speech timing calculation',
      'Full MOS 2.8.4 protocol compatibility for external rundown synchronization',
      'Instant teleprompter auto-sync upon story revision or rundown sequence change',
      'MAM asset search and 3D graphic template preview directly in the story editor'
    ]
  },
  {
    id: 'studio-production',
    label: 'Live Studio & OB Van Production',
    category: 'Live Switching & Multi-Cam',
    headline: '12-Channel Live Production Switcher & Multiview Wall',
    tagline: 'Multi-camera orchestration with hardware DVE wipes, 16-bus audio mixing, and dual DDRs',
    description:
      'Kashtrix Studio turns standard workstations into high-performance multi-camera control rooms. Operators manage up to 12 simultaneous live camera inputs, trigger hardware DVE and stinger wipes, mix multichannel audio with parametric EQ, and record uncompressed master archives while streaming live.',
    image: '/assets/products/studio-switcher-full.png',
    standards: ['12G/3G-SDI', 'NDI 6 High Profile', 'DVE Transitions', 'ProRes 422 HQ', 'EBU R128 EQ'],
    modules: [
      {
        name: 'Kashtrix Studio Switcher',
        slug: 'studio',
        badge: 'LIVE SWITCHER',
        desc: '12-channel multiview, audio EQ mixing desk & DDR video servers',
        icon: Video
      },
      {
        name: 'Multiview & QC Wall',
        slug: 'multiview',
        badge: 'CONFIDENCE',
        desc: 'Real-time multi-source monitor wall with freeze & silence alarms',
        icon: LayoutGrid
      },
      {
        name: 'Kashtrix 3D CG Editor',
        slug: 'cg-editor',
        badge: '3D COMPOSITOR',
        desc: 'Author lower thirds, tickers, and animated slabs with keyframe easing',
        icon: Layers
      }
    ],
    keyCapabilities: [
      '12-Channel live confidence multiview wall with tally indicators and audio peaks',
      'Hardware cut, auto-dissolve, 3D DVE squeeze, and alpha video stinger transitions',
      '16-Bus software mixing console with 3-band parametric EQ, compressor, and limiter',
      'Simultaneous ProRes/XDCAM broadcast master recording and multi-CDN streaming'
    ]
  },
  {
    id: 'streaming-ott',
    label: 'StreamOps Cloud OTT & Multi-CDN',
    category: 'Distribution & Transcoding',
    headline: 'High-Density Live Transcoder & Edge OTT Distribution',
    tagline: 'Adaptive bitrate ladders, low-latency HLS/DASH, and SCTE-35 server-side ad signaling',
    description:
      'StreamOps bridges linear television baseband transmission and the global OTT ecosystem. Transcode uncompressed SDI, ST 2110, or NDI feeds into pristine multi-bitrate HLS, MPEG-DASH, and SRT streams with NVIDIA NVENC hardware acceleration and multi-CDN failover routing.',
    image: '/assets/products/pro-io-st2110-scte.png',
    standards: ['LL-HLS', 'MPEG-DASH', 'CMAF', 'SRT Caller/Listener', 'NVENC AV1/HEVC', 'SCTE-35 SSAI'],
    modules: [
      {
        name: 'StreamOps OTT Engine',
        slug: 'streamops',
        badge: 'LIVE TRANSCODER',
        desc: 'Multi-profile ABR transcoding with hardware NVENC / QuickSync acceleration',
        icon: Radio
      },
      {
        name: 'Hardware Output Engine',
        slug: 'output-engine',
        badge: 'BASEBAND / IP',
        desc: 'DeckLink SDI, NDI 6, SRT, and DVB-TS transport stream multiplexer',
        icon: Radio
      },
      {
        name: 'Unified API Gateway',
        slug: 'api-gateway',
        badge: 'REST / WS BUS',
        desc: 'High-speed REST and WebSocket control bus for third-party automation',
        icon: Network
      }
    ],
    keyCapabilities: [
      'Hardware-accelerated HEVC, AV1, and H.264 multi-bitrate ladder transcoding',
      'Sub-3-second glass-to-glass latency with Low-Latency HLS and CMAF chunking',
      'SCTE-35 cue marker passthrough for server-side dynamic ad insertion (SSAI)',
      'Simultaneous edge routing across Akamai, Cloudflare, AWS CloudFront, and custom origins'
    ]
  },
  {
    id: 'operations-billing',
    label: 'Kashtrix OSS / BSS & Media Operations',
    category: 'Business & Asset Management',
    headline: 'Broadcast Operations Support & Automated Airtime Invoicing',
    tagline: 'As-run log reconciliation, advertiser invoicing, subscriber billing & MAM lifecycle',
    description:
      'Kashtrix OSS / BSS unites linear broadcast traffic, commercial airtime delivery, subscriber provisioning, and financial revenue. Reconcile Playout as-run transmission logs against advertiser contracts, generate automated PDF billing statements, and manage subscriber channel packages.',
    image: '/assets/products/channel-controller-cluster.png',
    standards: ['REST / GraphQL', 'PDF Invoicing', 'SHA-256 Checksums', 'PCI-DSS', 'MAM Storage SAN'],
    modules: [
      {
        name: 'Kashtrix OSS / BSS',
        slug: 'oss-bss',
        badge: 'BILLING & CRM',
        desc: 'Broadcast airtime invoicing, rate card yield management & provisioning',
        icon: CreditCard
      },
      {
        name: 'Enterprise MAM',
        slug: 'mam',
        badge: 'ASSET VAULT',
        desc: 'Permanent GUID tracking, automated proxies, SHA-256 hash & archive policies',
        icon: Database
      },
      {
        name: 'Ingest Server',
        slug: 'ingest-server',
        badge: 'MULTI-CAPTURE',
        desc: 'Scheduled SDI/NDI/SRT capture, compliance 24/7 logging & live input grid',
        icon: Radio
      }
    ],
    keyCapabilities: [
      'Automated reconciliation between Playout as-run logs and commercial advertising contracts',
      'Automated generation of verified PDF invoices for advertising agencies and sponsors',
      'Subscriber subscription lifecycle management with multi-tier channel package entitlements',
      'Automated MAM asset registration, technical metadata extraction, and proxy creation'
    ]
  }
];

export function SoftwareSolutionsMatrix() {
  const [activeDomain, setActiveDomain] = useState<WorkflowDomain>(WORKFLOW_DOMAINS[0]);

  return (
    <section className="py-20 bg-[var(--bg-surface)] border-y border-[var(--border-subtle)] transition-colors duration-200">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        
        {/* Section Header */}
        <div className="text-center max-w-3xl mx-auto mb-12">
          <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-[var(--bg-card)] border border-[var(--border-subtle)] text-xs font-bold text-[var(--accent)] mb-3 shadow-xs">
            <span className="w-2 h-2 rounded-full bg-[var(--accent)] inline-block" />
            <span>ENTERPRISE BROADCAST OPERATIONS</span>
          </div>
          <h2 className="text-3xl sm:text-4xl lg:text-5xl font-black text-[var(--text-main)] tracking-tight">
            Integrated Software Architectures
          </h2>
          <p className="mt-3 text-sm sm:text-base text-[var(--text-secondary)] leading-relaxed">
            Kashtrix eliminates software fragmentation. Select an operational environment to explore how our specialized broadcast engines coordinate in real time.
          </p>
        </div>

        {/* Operational Domain Navigation Tabs */}
        <div className="flex flex-wrap items-center justify-center gap-2 mb-10 p-1.5 rounded-2xl bg-[var(--bg-card)] border border-[var(--border-subtle)] max-w-4xl mx-auto shadow-xs">
          {WORKFLOW_DOMAINS.map((domain) => {
            const isActive = activeDomain.id === domain.id;
            return (
              <button
                key={domain.id}
                onClick={() => setActiveDomain(domain)}
                className={`relative px-4 py-2 rounded-xl text-xs font-bold transition-all cursor-pointer ${
                  isActive
                    ? 'text-white'
                    : 'text-[var(--text-muted)] hover:text-[var(--text-main)] hover:bg-[var(--bg-card-hover)]'
                }`}
              >
                {isActive && (
                  <motion.div
                    layoutId="activeDomainPill"
                    className="absolute inset-0 rounded-xl bg-[var(--accent)] shadow-xs"
                    transition={{ type: 'spring', duration: 0.35 }}
                  />
                )}
                <span className="relative z-10">{domain.label}</span>
              </button>
            );
          })}
        </div>

        {/* Active Domain Presentation Console */}
        <AnimatePresence mode="wait">
          <motion.div
            key={activeDomain.id}
            initial={{ opacity: 0, y: 15 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -15 }}
            transition={{ duration: 0.25 }}
            className="grid grid-cols-1 lg:grid-cols-12 gap-8 items-start"
          >
            {/* Left Column: Domain Details & Capabilities */}
            <div className="lg:col-span-6 space-y-6">
              
              <div>
                <div className="flex items-center gap-2 mb-2">
                  <span className="px-2 py-0.5 rounded text-[10px] font-mono font-bold uppercase tracking-wider bg-[var(--accent)]/10 text-[var(--accent)] border border-[var(--accent)]/20">
                    {activeDomain.category}
                  </span>
                </div>
                <h3 className="text-2xl sm:text-3xl font-black text-[var(--text-main)] tracking-tight">
                  {activeDomain.headline}
                </h3>
                <p className="mt-1 text-sm font-semibold text-[var(--accent)]">
                  {activeDomain.tagline}
                </p>
                <p className="mt-3 text-xs sm:text-sm text-[var(--text-secondary)] leading-relaxed">
                  {activeDomain.description}
                </p>
              </div>

              {/* Certified Standards Badges */}
              <div>
                <div className="text-[11px] font-bold text-[var(--text-muted)] uppercase tracking-wider mb-2">
                  Certified Standards & Protocols
                </div>
                <div className="flex flex-wrap gap-1.5">
                  {activeDomain.standards.map((std, i) => (
                    <span
                      key={i}
                      className="px-2 py-1 rounded-md text-[11px] font-mono font-semibold bg-[var(--bg-card)] text-[var(--text-main)] border border-[var(--border-subtle)]"
                    >
                      {std}
                    </span>
                  ))}
                </div>
              </div>

              {/* Key Technical Capabilities */}
              <div className="space-y-2">
                <div className="text-[11px] font-bold text-[var(--text-muted)] uppercase tracking-wider">
                  Operational Highlights
                </div>
                {activeDomain.keyCapabilities.map((cap, i) => (
                  <div key={i} className="flex items-start gap-2.5">
                    <CheckCircle2 className="w-4 h-4 text-emerald-500 shrink-0 mt-0.5" />
                    <span className="text-xs text-[var(--text-secondary)] leading-relaxed">
                      {cap}
                    </span>
                  </div>
                ))}
              </div>

              {/* Software Modules in this Architecture */}
              <div className="pt-2">
                <div className="text-[11px] font-bold text-[var(--text-muted)] uppercase tracking-wider mb-3">
                  Included Software Engines (Click to Open Dedicated Single Page):
                </div>
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-2.5">
                  {activeDomain.modules.map((mod) => {
                    const ModIcon = mod.icon;
                    return (
                      <Link
                        key={mod.slug}
                        href={`/products/${mod.slug}`}
                        className="p-3 rounded-xl bg-[var(--bg-card)] hover:bg-[var(--bg-card-hover)] border border-[var(--border-subtle)] hover:border-[var(--accent)] transition-all group shadow-xs flex items-start gap-2.5"
                      >
                        <div className="p-1.5 rounded-lg bg-[var(--bg-surface)] text-[var(--accent)] shrink-0 border border-[var(--border-subtle)] group-hover:scale-105 transition-transform">
                          <ModIcon className="w-4 h-4" />
                        </div>
                        <div className="min-w-0 flex-1">
                          <div className="flex items-center justify-between gap-1">
                            <span className="text-xs font-bold text-[var(--text-main)] group-hover:text-[var(--accent)] truncate">
                              {mod.name}
                            </span>
                            <span className="text-[9px] font-semibold px-1 py-0.2 rounded bg-[var(--bg-page)] text-[var(--text-muted)] border border-[var(--border-subtle)]">
                              {mod.badge}
                            </span>
                          </div>
                          <p className="text-[10px] text-[var(--text-muted)] line-clamp-1 mt-0.5">
                            {mod.desc}
                          </p>
                          <div className="flex items-center gap-1 text-[10px] font-bold text-[var(--accent)] mt-1 opacity-0 group-hover:opacity-100 transition-opacity">
                            <span>Open Single Page & Specs</span>
                            <ArrowRight className="w-3 h-3" />
                          </div>
                        </div>
                      </Link>
                    );
                  })}
                </div>
              </div>

            </div>

            {/* Right Column: Screenshot & Console Display */}
            <div className="lg:col-span-6">
              <div className="rounded-2xl p-2.5 bg-[var(--bg-card)] border border-[var(--border-subtle)] shadow-lg">
                <div className="flex items-center justify-between px-3 py-2 bg-[var(--bg-surface)] rounded-t-xl border-b border-[var(--border-subtle)]">
                  <div className="flex items-center gap-1.5">
                    <span className="w-2.5 h-2.5 rounded-full bg-[#EF4444]" />
                    <span className="w-2.5 h-2.5 rounded-full bg-[#F59E0B]" />
                    <span className="w-2.5 h-2.5 rounded-full bg-[#10B981]" />
                    <span className="ml-2 text-xs font-mono font-bold text-[var(--text-main)] truncate">
                      {activeDomain.label}
                    </span>
                  </div>
                  <span className="text-[10px] font-mono text-emerald-600 dark:text-emerald-400 font-bold">
                    • VERIFIED WORKFLOW
                  </span>
                </div>

                <ScreenshotCard
                  src={activeDomain.image}
                  alt={activeDomain.headline}
                  title={activeDomain.headline}
                  caption={activeDomain.tagline}
                  className="rounded-b-xl border-x-0 border-t-0"
                />

                <div className="p-3 bg-[var(--bg-surface)] rounded-b-xl border-t border-[var(--border-subtle)] flex items-center justify-between">
                  <span className="text-[11px] text-[var(--text-muted)] font-mono">
                    DirectX 11 Hardware Rendered • SDI / ST 2110 / NDI 6
                  </span>
                  <Link
                    href={`/products/${activeDomain.modules[0].slug}`}
                    className="inline-flex items-center gap-1 text-xs font-bold text-[var(--accent)] hover:underline"
                  >
                    <span>View Primary Engine ({activeDomain.modules[0].name}) →</span>
                  </Link>
                </div>
              </div>
            </div>

          </motion.div>
        </AnimatePresence>

      </div>
    </section>
  );
}
