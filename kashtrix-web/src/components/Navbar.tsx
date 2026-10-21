'use client';

import React, { useState, useEffect } from 'react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { useTheme } from '@/components/ThemeProvider';
import { 
  Tv, 
  Menu, 
  X, 
  ChevronDown, 
  Sparkles, 
  Radio, 
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
  Video, 
  Settings, 
  ArrowRight,
  Sun,
  Moon,
  CreditCard,
  Globe
} from 'lucide-react';

const PRODUCTS_NAVIGATION: {
  category: string;
  items: { slug: string; name: string; badge: string; icon: React.ElementType; desc: string }[];
}[] = [
  {
    category: 'Playout & Automation',
    items: [
      {
        slug: 'playout',
        name: 'Kashtrix Playout',
        badge: 'FLAGSHIP',
        icon: Tv,
        desc: '24/7 mission-critical playout engine with Program/Preview buses'
      },
      {
        slug: 'scheduler',
        name: 'Scheduler & Traffic',
        badge: 'BXF 24/7',
        icon: Calendar,
        desc: 'Linear 24/7 day timeline, commercial breaks & as-run reconciliation'
      },
      {
        slug: 'channel-controller',
        name: 'Channel Controller',
        badge: 'CLUSTER',
        icon: Network,
        desc: 'Multi-station orchestration grid supervising remote playout nodes'
      }
    ]
  },
  {
    category: 'Live Production',
    items: [
      {
        slug: 'studio',
        name: 'Kashtrix Studio',
        badge: 'LIVE SWITCHER',
        icon: Video,
        desc: '12-channel multiview, audio mixer, DDRs, DVE & live streaming'
      },
      {
        slug: 'multiview',
        name: 'Multiview & QC',
        badge: 'CONFIDENCE',
        icon: LayoutGrid,
        desc: 'Real-time multi-source monitor wall with EBU R128 loudness meters'
      },
      {
        slug: 'prompter',
        name: 'Teleprompter',
        badge: 'TALENT SYNC',
        icon: TvMinimalPlay,
        desc: 'GPU-accelerated smooth scrolling linked to newsroom stories'
      }
    ]
  },
  {
    category: 'Graphics & CG',
    items: [
      {
        slug: 'cg-editor',
        name: 'Kashtrix CG Editor',
        badge: '3D AUTHORING',
        icon: Layers,
        desc: '3D geometry transforms, bezier easing curves & dynamic fields'
      },
      {
        slug: 'cg-controller',
        name: 'Kashtrix CG Controller',
        badge: 'ON-AIR SURFACE',
        icon: Sparkles,
        desc: 'Dual preview/program graphics bus with quick-edit lower thirds'
      }
    ]
  },
  {
    category: 'Media & Ingest',
    items: [
      {
        slug: 'mam',
        name: 'Kashtrix MAM',
        badge: 'ENTERPRISE',
        icon: Database,
        desc: 'Media asset management, automated proxies, SHA-256 & QC'
      },
      {
        slug: 'ingest-server',
        name: 'Ingest Server',
        badge: 'MULTI-SOURCE',
        icon: HardDrive,
        desc: 'Scheduled SDI/NDI/SRT capture, compliance & live input grid'
      }
    ]
  },
  {
    category: 'Newsroom & MOS',
    items: [
      {
        slug: 'nrcs-web',
        name: 'Kashtrix NRCS Web',
        badge: 'CLOUD MOS',
        icon: FileText,
        desc: 'Browser-native newsroom editor with MOS 2.8.4 HTML5 plugin'
      },
      {
        slug: 'nrcs',
        name: 'Kashtrix NRCS Core',
        badge: 'DESKTOP MOS',
        icon: FileText,
        desc: 'Complete newsroom computer system with live rundown publishing'
      }
    ]
  },
  {
    category: 'Streaming & Business',
    items: [
      {
        slug: 'streamops',
        name: 'StreamOps OTT',
        badge: 'MULTI-CDN',
        icon: Radio,
        desc: 'Cloud & hybrid live streaming, multi-profile transcoder & SCTE-35'
      },
      {
        slug: 'oss-bss',
        name: 'Kashtrix OSS / BSS',
        badge: 'BILLING',
        icon: CreditCard,
        desc: 'Operations support, subscriber lifecycle & automated traffic invoicing'
      },
      {
        slug: 'ha-redundancy',
        name: 'HA Redundancy',
        badge: '1+1 FAILOVER',
        icon: ShieldCheck,
        desc: 'Zero-downtime hot backup mirror & SMPTE ST 2022-7 hitless'
      },
      {
        slug: 'api-gateway',
        name: 'API Gateway & Web',
        badge: 'REST / WS',
        icon: Code2,
        desc: 'Unified broadcast control bus on port 9080 & web client suite'
      }
    ]
  }
];

export function Navbar() {
  const [productsOpen, setProductsOpen] = useState(false);
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);
  const [scrolled, setScrolled] = useState(false);
  const pathname = usePathname();
  const { theme, toggleTheme } = useTheme();

  useEffect(() => {
    const handleScroll = () => setScrolled(window.scrollY > 20);
    window.addEventListener('scroll', handleScroll);
    return () => window.removeEventListener('scroll', handleScroll);
  }, []);

  useEffect(() => {
    setProductsOpen(false);
    setMobileMenuOpen(false);
  }, [pathname]);

  return (
    <header
      className={`fixed top-0 left-0 right-0 z-50 transition-all duration-200 ${
        scrolled
          ? 'bg-[var(--bg-surface)]/95 backdrop-blur-md shadow-sm border-b border-[var(--border-subtle)]'
          : 'bg-[var(--bg-page)]/80 backdrop-blur-xs border-b border-[var(--border-subtle)]'
      }`}
    >
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex items-center justify-between h-18">
          
          {/* Kashtrix Brand Logo */}
          <Link href="/" className="flex items-center gap-3 group">
            <div className="relative flex items-center justify-center w-10 h-10 rounded-xl bg-[var(--accent)] text-white shadow-sm transition-transform duration-200 group-hover:scale-105">
              <Tv className="w-5 h-5 text-white" />
              <span className="absolute top-1.5 right-1.5 w-2 h-2 rounded-full bg-emerald-400 ring-2 ring-white dark:ring-slate-900" />
            </div>

            <div className="flex flex-col">
              <div className="flex items-center gap-2">
                <span className="text-xl font-black tracking-tight text-[var(--text-main)] group-hover:text-[var(--accent)] transition-colors">
                  KASHTRIX
                </span>
                <span className="px-1.5 py-0.5 text-[9px] font-mono font-bold uppercase tracking-wider rounded bg-[var(--accent)]/10 text-[var(--accent)] border border-[var(--accent)]/25">
                  SOFTWARE SUITE
                </span>
              </div>
              <div className="flex items-center gap-1.5 text-[9px] sm:text-[10px] tracking-wider font-semibold text-[var(--text-muted)] uppercase">
                <span className="w-1.5 h-1.5 rounded-full bg-emerald-500 inline-block" />
                <span>BROADCAST & MEDIA ENGINE</span>
              </div>
            </div>
          </Link>

          {/* Desktop Navigation */}
          <nav className="hidden lg:flex items-center gap-1">
            <Link
              href="/"
              className={`px-3 py-2 rounded-lg text-xs font-bold transition-all ${
                pathname === '/'
                  ? 'text-[var(--accent)] bg-[var(--accent)]/10 font-bold'
                  : 'text-[var(--text-secondary)] hover:text-[var(--text-main)] hover:bg-[var(--bg-card-hover)]'
              }`}
            >
              Overview
            </Link>

            {/* Products Mega Dropdown */}
            <div className="relative">
              <button
                onClick={() => setProductsOpen(!productsOpen)}
                onMouseEnter={() => setProductsOpen(true)}
                className={`flex items-center gap-1 px-3 py-2 rounded-lg text-xs font-bold transition-all cursor-pointer ${
                  productsOpen || pathname.startsWith('/products')
                    ? 'text-[var(--accent)] bg-[var(--accent)]/10'
                    : 'text-[var(--text-secondary)] hover:text-[var(--text-main)] hover:bg-[var(--bg-card-hover)]'
                }`}
              >
                <span>Products & Modules</span>
                <ChevronDown
                  className={`w-3.5 h-3.5 transition-transform duration-200 ${productsOpen ? 'rotate-180 text-[var(--accent)]' : ''}`}
                />
              </button>

              {productsOpen && (
                <div
                  onMouseLeave={() => setProductsOpen(false)}
                  className="absolute top-full -left-48 w-[980px] mt-2 rounded-2xl bg-[var(--bg-card)] border border-[var(--border-strong)] shadow-xl p-6 grid grid-cols-3 gap-6 animate-in fade-in slide-in-from-top-2 duration-150"
                >
                  {PRODUCTS_NAVIGATION.map((cat, idx) => (
                    <div key={idx} className="space-y-2.5">
                      <div className="text-[11px] font-bold uppercase tracking-wider text-[var(--accent)] border-b border-[var(--border-subtle)] pb-1.5">
                        {cat.category}
                      </div>
                      <div className="space-y-1">
                        {cat.items.map((prod) => {
                          const IconCmp = prod.icon;
                          const active = pathname === `/products/${prod.slug}`;
                          return (
                            <Link
                              key={prod.slug}
                              href={`/products/${prod.slug}`}
                              className={`group flex items-start gap-2.5 p-2 rounded-xl transition-all ${
                                active
                                  ? 'bg-[var(--accent)]/10 border border-[var(--accent)]/30'
                                  : 'hover:bg-[var(--bg-card-hover)]'
                              }`}
                            >
                              <div className="mt-0.5 p-1.5 rounded-lg bg-[var(--bg-surface)] border border-[var(--border-subtle)] group-hover:border-[var(--accent)] transition-colors shrink-0 text-[var(--accent)]">
                                <IconCmp className="w-3.5 h-3.5" />
                              </div>
                              <div className="flex-1 min-w-0">
                                <div className="flex items-center gap-1.5">
                                  <span className="text-xs font-bold text-[var(--text-main)] group-hover:text-[var(--accent)] truncate">
                                    {prod.name}
                                  </span>
                                  <span className="text-[9px] font-semibold px-1 py-0.2 rounded bg-[var(--bg-page)] text-[var(--text-muted)] border border-[var(--border-subtle)]">
                                    {prod.badge}
                                  </span>
                                </div>
                                <p className="text-[11px] text-[var(--text-muted)] line-clamp-1 leading-snug">
                                  {prod.desc}
                                </p>
                              </div>
                            </Link>
                          );
                        })}
                      </div>
                    </div>
                  ))}

                  <div className="col-span-3 pt-3 border-t border-[var(--border-subtle)] flex items-center justify-between text-xs text-[var(--text-muted)]">
                    <div className="flex items-center gap-2">
                      <span className="w-2 h-2 rounded-full bg-emerald-500" />
                      <span>All 17 Software Modules Integrated with Kashtrix API Gateway & MOS 2.8.4</span>
                    </div>
                    <Link
                      href="/products"
                      className="font-bold text-[var(--accent)] hover:underline flex items-center gap-1 text-xs"
                    >
                      <span>View All 17 Software Products Table</span>
                      <ArrowRight className="w-3.5 h-3.5" />
                    </Link>
                  </div>
                </div>
              )}
            </div>

            <Link
              href="/architecture"
              className={`px-3 py-2 rounded-lg text-xs font-bold transition-all ${
                pathname === '/architecture'
                  ? 'text-[var(--accent)] bg-[var(--accent)]/10'
                  : 'text-[var(--text-secondary)] hover:text-[var(--text-main)] hover:bg-[var(--bg-card-hover)]'
              }`}
            >
              Architecture & I/O
            </Link>

            <Link
              href="/admin"
              className={`px-3 py-2 rounded-lg text-xs font-bold transition-all flex items-center gap-1.5 ${
                pathname === '/admin'
                  ? 'text-[var(--accent)] bg-[var(--accent)]/10'
                  : 'text-[var(--text-secondary)] hover:text-[var(--text-main)] hover:bg-[var(--bg-card-hover)]'
              }`}
            >
              <span className="w-1.5 h-1.5 rounded-full bg-emerald-500" />
              <span>Admin MCR</span>
            </Link>

            <Link
              href="/contact"
              className={`px-3 py-2 rounded-lg text-xs font-bold transition-all ${
                pathname === '/contact'
                  ? 'text-[var(--accent)] bg-[var(--accent)]/10'
                  : 'text-[var(--text-secondary)] hover:text-[var(--text-main)] hover:bg-[var(--bg-card-hover)]'
              }`}
            >
              Contact & Quote
            </Link>
          </nav>

          {/* Right Action Buttons */}
          <div className="hidden sm:flex items-center gap-2.5">
            {/* Theme Toggle Button */}
            <button
              onClick={toggleTheme}
              aria-label="Toggle theme"
              className="p-2 rounded-xl bg-[var(--bg-card)] border border-[var(--border-subtle)] text-[var(--text-secondary)] hover:text-[var(--text-main)] hover:border-[var(--accent)] transition-all cursor-pointer shadow-xs"
              title={theme === 'dark' ? 'Switch to Light Mode' : 'Switch to Dark Mode'}
            >
              {theme === 'dark' ? (
                <Sun className="w-4 h-4 text-amber-400" />
              ) : (
                <Moon className="w-4 h-4 text-[var(--accent)]" />
              )}
            </button>

            {/* Request Demo Primary CTA */}
            <Link
              href="/contact"
              className="px-4 py-2 rounded-xl text-xs font-bold text-white bg-[var(--accent)] hover:bg-[var(--accent-dark)] hover:shadow-md hover:shadow-[var(--accent)]/20 transition-all flex items-center gap-1.5 cursor-pointer"
            >
              <Sparkles className="w-3.5 h-3.5 text-white" />
              <span>Request Demo</span>
            </Link>
          </div>

          {/* Mobile Menu Button */}
          <div className="flex items-center gap-2 lg:hidden">
            <button
              onClick={toggleTheme}
              aria-label="Toggle theme"
              className="p-2 rounded-lg bg-[var(--bg-card)] border border-[var(--border-subtle)] text-[var(--text-secondary)]"
            >
              {theme === 'dark' ? <Sun className="w-4 h-4 text-amber-400" /> : <Moon className="w-4 h-4 text-[var(--accent)]" />}
            </button>

            <button
              onClick={() => setMobileMenuOpen(!mobileMenuOpen)}
              className="p-2 rounded-lg bg-[var(--bg-card)] border border-[var(--border-subtle)] text-[var(--text-main)]"
            >
              {mobileMenuOpen ? <X className="w-5 h-5" /> : <Menu className="w-5 h-5" />}
            </button>
          </div>

        </div>
      </div>

      {/* Mobile Drawer */}
      {mobileMenuOpen && (
        <div className="lg:hidden bg-[var(--bg-card)] border-b border-[var(--border-subtle)] px-4 pt-3 pb-6 space-y-3 max-h-[80vh] overflow-y-auto">
          <Link
            href="/"
            className="block px-3 py-2 rounded-lg text-sm font-semibold hover:bg-[var(--bg-card-hover)]"
          >
            Overview
          </Link>
          <Link
            href="/products"
            className="block px-3 py-2 rounded-lg text-sm font-semibold text-[var(--accent)] font-bold hover:bg-[var(--bg-card-hover)]"
          >
            All 17 Products & Modules →
          </Link>
          <div className="pl-3 border-l-2 border-[var(--border-subtle)] space-y-1.5 my-2">
            {PRODUCTS_NAVIGATION.flatMap((c) => c.items).slice(0, 8).map((p) => (
              <Link
                key={p.slug}
                href={`/products/${p.slug}`}
                className="block text-xs text-[var(--text-secondary)] hover:text-[var(--text-main)] py-1 truncate"
              >
                • {p.name}
              </Link>
            ))}
          </div>
          <Link
            href="/architecture"
            className="block px-3 py-2 rounded-lg text-sm font-semibold hover:bg-[var(--bg-card-hover)]"
          >
            Architecture & I/O
          </Link>
          <Link
            href="/admin"
            className="block px-3 py-2 rounded-lg text-sm font-semibold hover:bg-[var(--bg-card-hover)]"
          >
            Admin MCR CMS
          </Link>
          <Link
            href="/contact"
            className="block px-3 py-2 rounded-lg text-sm font-semibold text-white bg-[var(--accent)] text-center rounded-xl"
          >
            Request Engineering Demo
          </Link>
        </div>
      )}
    </header>
  );
}
