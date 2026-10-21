'use client';

import React, { useState } from 'react';
import Link from 'next/link';
import { 
  Tv, 
  Video, 
  Layers, 
  Database, 
  FileText, 
  TvMinimalPlay, 
  Network, 
  HardDrive, 
  LayoutGrid, 
  Calendar, 
  ShieldCheck, 
  Code2, 
  Radio, 
  CreditCard, 
  ArrowRight, 
  Search, 
  ExternalLink,
  Sparkles
} from 'lucide-react';

interface ProductEntry {
  slug: string;
  name: string;
  category: string;
  badge: string;
  icon: React.ElementType;
  primaryStandard: string;
  summary: string;
}

const ALL_SOFTWARE_PRODUCTS: ProductEntry[] = [
  {
    slug: 'playout',
    name: 'Kashtrix Playout Engine',
    category: 'Core Playout',
    badge: 'FLAGSHIP',
    icon: Tv,
    primaryStandard: 'DirectX 11 • SMPTE ST 2110 • SDI 12G • SCTE-35',
    summary: '24/7 continuous master control playout with independent Program & Preview buses and secondary event automation.'
  },
  {
    slug: 'studio',
    name: 'Kashtrix Studio Switcher',
    category: 'Live Production',
    badge: 'LIVE SWITCHER',
    icon: Video,
    primaryStandard: '12-Ch Live Video • DVE Transitions • 16-Bus EQ Audio',
    summary: 'Integrated live production switcher with confidence multiview, hardware DVE wipes, DDR servers and master recording.'
  },
  {
    slug: 'cg-editor',
    name: 'Kashtrix 3D CG Editor',
    category: 'Graphics & CG',
    badge: '3D AUTHORING',
    icon: Layers,
    primaryStandard: 'Keyframe Timeline • 3D Transforms • Dynamic Data JSON',
    summary: 'Visual broadcast motion graphics template designer with multi-track keyframe timelines and live data bindings.'
  },
  {
    slug: 'cg-controller',
    name: 'Kashtrix CG Controller',
    category: 'Graphics & CG',
    badge: 'ON-AIR SURFACE',
    icon: Sparkles,
    primaryStandard: 'Dual PVW/PGM Graphics • Quick-Edit Lower Thirds',
    summary: 'On-air graphics playout surface allowing operators to fire lower thirds, bug logos, tickers, and slabs.'
  },
  {
    slug: 'nrcs-web',
    name: 'Kashtrix NRCS Web',
    category: 'Newsroom & MOS',
    badge: 'CLOUD MOS',
    icon: FileText,
    primaryStandard: 'Cloud Browser • MOS 2.8.4 HTML5 • Auto-Cume Timing',
    summary: 'Modern web-based newsroom system for reporters to script stories, bind MAM media, and push rundowns live.'
  },
  {
    slug: 'nrcs',
    name: 'Kashtrix NRCS Core',
    category: 'Newsroom & MOS',
    badge: 'DESKTOP MOS',
    icon: FileText,
    primaryStandard: 'MOS 2.8.4 Protocol • Avid iNEWS / ENPS Sync',
    summary: 'Enterprise television newsroom computer system with bulletin rundown sequencing and MOS object attachments.'
  },
  {
    slug: 'streamops',
    name: 'StreamOps OTT Engine',
    category: 'Streaming & OTT',
    badge: 'MULTI-CDN OTT',
    icon: Radio,
    primaryStandard: 'Low-Latency HLS • MPEG-DASH • NVENC • SCTE-35 SSAI',
    summary: 'Multi-profile live video transcoder and OTT distribution engine with multi-CDN failover and dynamic ad signaling.'
  },
  {
    slug: 'oss-bss',
    name: 'Kashtrix OSS / BSS',
    category: 'Business & Telecom',
    badge: 'BILLING & CRM',
    icon: CreditCard,
    primaryStandard: 'As-Run Invoicing • Contract Reconciliation • REST API',
    summary: 'Operations and business support system automating broadcast airtime billing, subscriber plans, and channel provisioning.'
  },
  {
    slug: 'mam',
    name: 'Kashtrix Enterprise MAM',
    category: 'Media Management',
    badge: 'MEDIA VAULT',
    icon: Database,
    primaryStandard: 'Permanent GUID • Automated Proxies • SHA-256 Hashes',
    summary: 'Central broadcast media asset catalog with proxy encoding, compliance QC gating, and deep metadata indexing.'
  },
  {
    slug: 'ingest-server',
    name: 'Kashtrix Ingest Server',
    category: 'Media Management',
    badge: 'MULTI-CAPTURE',
    icon: HardDrive,
    primaryStandard: 'SDI / NDI / SRT • Scheduled & Crash • 24/7 Logging',
    summary: 'Multi-channel broadcast capture engine for satellite feeds, live cameras, recurring calendar ingest, and compliance.'
  },
  {
    slug: 'prompter',
    name: 'Kashtrix Teleprompter',
    category: 'Newsroom & Studio',
    badge: 'TALENT SYNC',
    icon: TvMinimalPlay,
    primaryStandard: 'GPU Smooth Scroll • MOS Rundown Sync • Multi-Screen',
    summary: 'Broadcast studio teleprompter engine with bi-directional newsroom synchronization and variable speed controllers.'
  },
  {
    slug: 'multiview',
    name: 'Kashtrix Multiview Wall & QC',
    category: 'Monitoring & QC',
    badge: 'CONFIDENCE',
    icon: LayoutGrid,
    primaryStandard: 'SMPTE ST 2110 / SDI • EBU R128 Loudness • Black Alarms',
    summary: 'Zero-drop multi-source confidence monitor wall with real-time video freeze, black frame, and silence detection.'
  },
  {
    slug: 'channel-controller',
    name: 'Master Channel Controller',
    category: 'Core Playout',
    badge: 'CLUSTER GRID',
    icon: Network,
    primaryStandard: 'Multi-Station Grid • Remote Node Supervision • SNMP',
    summary: 'Master control room management surface to monitor and orchestrate multiple simultaneous playout channels.'
  },
  {
    slug: 'scheduler',
    name: 'Scheduler & Traffic Master',
    category: 'Core Playout',
    badge: 'BXF 24/7',
    icon: Calendar,
    primaryStandard: 'SMPTE 2021 BXF • 24/7 Day Timeline • As-Run Export',
    summary: 'Linear television day scheduling with commercial break sequencing, category color-coding, and traffic import.'
  },
  {
    slug: 'ha-redundancy',
    name: 'HA Redundancy Controller',
    category: 'Infrastructure',
    badge: '1+1 FAILOVER',
    icon: ShieldCheck,
    primaryStandard: 'SMPTE ST 2022-7 Hitless • UDP Heartbeat • Auto Bypass',
    summary: 'Zero-downtime hot backup mirror and N+M shared cluster controller with sub-second automated transmission failover.'
  },
  {
    slug: 'output-engine',
    name: 'Hardware Output Engine',
    category: 'Infrastructure',
    badge: 'DVB-TS / SDI',
    icon: Radio,
    primaryStandard: 'DeckLink 8K Pro • NDI 6 • SRT • DVB SI/PSI Tables',
    summary: 'Direct hardware baseband video output and DVB transport stream multiplexer with table generation.'
  },
  {
    slug: 'api-gateway',
    name: 'Unified API Gateway',
    category: 'Infrastructure',
    badge: 'REST / WS / MCP',
    icon: Code2,
    primaryStandard: 'Port 9080 • REST • WebSockets • TCP JSONL • MCP',
    summary: 'Central broadcast integration bus exposing high-speed REST and WebSocket APIs for automation and web clients.'
  }
];

export function SoftwareProductsDirectory() {
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedCategory, setSelectedCategory] = useState('All');

  const categories = ['All', 'Core Playout', 'Live Production', 'Graphics & CG', 'Newsroom & MOS', 'Streaming & OTT', 'Business & Telecom', 'Media Management', 'Monitoring & QC', 'Infrastructure'];

  const filteredProducts = ALL_SOFTWARE_PRODUCTS.filter((prod) => {
    const matchesSearch =
      prod.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
      prod.primaryStandard.toLowerCase().includes(searchQuery.toLowerCase()) ||
      prod.summary.toLowerCase().includes(searchQuery.toLowerCase());
    const matchesCategory =
      selectedCategory === 'All' || prod.category.includes(selectedCategory);
    return matchesSearch && matchesCategory;
  });

  return (
    <section className="py-20 bg-[var(--bg-page)] transition-colors duration-200">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        
        {/* Section Header */}
        <div className="flex flex-col md:flex-row md:items-end justify-between gap-4 mb-10">
          <div>
            <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-[var(--bg-card)] border border-[var(--border-subtle)] text-xs font-bold text-[var(--accent)] mb-2 shadow-xs">
              <span className="w-2 h-2 rounded-full bg-emerald-500 inline-block" />
              <span>COMPLETE SOFTWARE MATRIX</span>
            </div>
            <h2 className="text-3xl sm:text-4xl font-black text-[var(--text-main)] tracking-tight">
              All 17 Dedicated Software Applications
            </h2>
            <p className="mt-2 text-xs sm:text-sm text-[var(--text-secondary)]">
              Every engine features its own dedicated single page with in-depth technical datasheets, full specs, and download requests.
            </p>
          </div>

          {/* Search Input */}
          <div className="relative w-full md:w-72">
            <Search className="w-4 h-4 text-[var(--text-muted)] absolute left-3 top-1/2 -translate-y-1/2" />
            <input
              type="text"
              placeholder="Search software or standards..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="w-full pl-9 pr-3 py-2 rounded-xl text-xs bg-[var(--bg-card)] border border-[var(--border-subtle)] focus:border-[var(--accent)] focus:outline-hidden text-[var(--text-main)] shadow-xs"
            />
          </div>
        </div>

        {/* Category Filter Pills */}
        <div className="flex flex-wrap items-center gap-1.5 mb-8">
          {categories.map((cat) => (
            <button
              key={cat}
              onClick={() => setSelectedCategory(cat)}
              className={`px-3 py-1.5 rounded-lg text-xs font-bold transition-all cursor-pointer ${
                selectedCategory === cat
                  ? 'bg-[var(--accent)] text-white shadow-xs'
                  : 'bg-[var(--bg-card)] text-[var(--text-muted)] hover:text-[var(--text-main)] hover:bg-[var(--bg-card-hover)] border border-[var(--border-subtle)]'
              }`}
            >
              {cat}
            </button>
          ))}
        </div>

        {/* High-Density Software Products Matrix Table */}
        <div className="rounded-2xl border border-[var(--border-subtle)] bg-[var(--bg-card)] shadow-xs overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs border-collapse">
              <thead>
                <tr className="bg-[var(--bg-surface)] border-b border-[var(--border-subtle)] text-[var(--text-muted)] font-mono uppercase tracking-wider text-[10px]">
                  <th className="py-3.5 px-4 font-bold">Software Product</th>
                  <th className="py-3.5 px-4 font-bold hidden sm:table-cell">Category</th>
                  <th className="py-3.5 px-4 font-bold hidden md:table-cell">Key Specifications & Protocols</th>
                  <th className="py-3.5 px-4 font-bold hidden lg:table-cell">Primary Function</th>
                  <th className="py-3.5 px-4 font-bold text-right">Dedicated Single Page</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[var(--border-subtle)]">
                {filteredProducts.map((prod) => {
                  const ProdIcon = prod.icon;
                  return (
                    <tr
                      key={prod.slug}
                      className="hover:bg-[var(--bg-card-hover)] transition-colors group"
                    >
                      {/* Product Name & Icon */}
                      <td className="py-3.5 px-4">
                        <Link
                          href={`/products/${prod.slug}`}
                          className="flex items-center gap-3 group-hover:text-[var(--accent)] transition-colors"
                        >
                          <div className="p-2 rounded-lg bg-[var(--bg-surface)] text-[var(--accent)] border border-[var(--border-subtle)] shrink-0">
                            <ProdIcon className="w-4 h-4" />
                          </div>
                          <div>
                            <div className="font-bold text-xs text-[var(--text-main)] group-hover:text-[var(--accent)] transition-colors">
                              {prod.name}
                            </div>
                            <span className="inline-block mt-0.5 px-1.5 py-0.2 rounded text-[9px] font-mono font-bold bg-[var(--accent)]/10 text-[var(--accent)]">
                              {prod.badge}
                            </span>
                          </div>
                        </Link>
                      </td>

                      {/* Category */}
                      <td className="py-3.5 px-4 hidden sm:table-cell font-medium text-[var(--text-secondary)]">
                        {prod.category}
                      </td>

                      {/* Key Standards */}
                      <td className="py-3.5 px-4 hidden md:table-cell font-mono text-[11px] text-[var(--text-muted)]">
                        {prod.primaryStandard}
                      </td>

                      {/* Primary Function */}
                      <td className="py-3.5 px-4 hidden lg:table-cell text-[var(--text-secondary)] max-w-xs truncate">
                        {prod.summary}
                      </td>

                      {/* Link to Dedicated Page */}
                      <td className="py-3.5 px-4 text-right">
                        <Link
                          href={`/products/${prod.slug}`}
                          className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-bold text-[var(--accent)] hover:text-white hover:bg-[var(--accent)] border border-[var(--border-subtle)] hover:border-[var(--accent)] transition-all shadow-xs"
                        >
                          <span>Open Single Page</span>
                          <ArrowRight className="w-3 h-3" />
                        </Link>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </div>

      </div>
    </section>
  );
}
