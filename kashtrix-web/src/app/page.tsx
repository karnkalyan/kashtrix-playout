import React from 'react';
import Link from 'next/link';
import { prisma } from '@/lib/prisma';
import { Navbar } from '@/components/Navbar';
import { Footer } from '@/components/Footer';
import { BroadcastHero } from '@/components/BroadcastHero';
import { StandardsTicker } from '@/components/StandardsTicker';
import { SoftwareSolutionsMatrix } from '@/components/SoftwareSolutionsMatrix';
import { SoftwareProductsDirectory } from '@/components/SoftwareProductsDirectory';
import { BroadcastSignalFlow } from '@/components/BroadcastSignalFlow';
import { 
  Tv, 
  Video, 
  Layers, 
  Database, 
  FileText, 
  ShieldCheck, 
  Radio, 
  ArrowRight, 
  CheckCircle2, 
  Server, 
  Cpu, 
  Activity, 
  Clock, 
  Quote,
  Sparkles,
  Zap,
  Check,
  HardDrive,
  CreditCard,
  Network
} from 'lucide-react';

export const revalidate = 0;

export default async function HomePage() {
  const heroContent = await prisma.heroContent.findFirst();
  const testimonials = await prisma.testimonial.findMany({
    orderBy: { order: 'asc' }
  });

  return (
    <div className="min-h-screen bg-[var(--bg-page)] text-[var(--text-main)] transition-colors duration-200">
      <Navbar />

      {/* Hero Section with 8-Tab Software Workstation Console */}
      <BroadcastHero content={heroContent || undefined} />

      {/* Broadcast Standards & Protocol Ticker */}
      <StandardsTicker />

      {/* 1. Software Solutions By Operational Domain (NOT Service Cards!) */}
      <SoftwareSolutionsMatrix />

      {/* 2. Interactive Broadcast Architecture Workflow Pipeline */}
      <section className="py-20 max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="text-center max-w-3xl mx-auto mb-12">
          <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-[var(--bg-card)] border border-[var(--border-subtle)] text-xs font-bold text-[var(--accent)] mb-3 shadow-xs">
            <Activity className="w-3.5 h-3.5 text-emerald-500" />
            <span>END-TO-END TELEVISION PIPELINE</span>
          </div>
          <h2 className="text-3xl sm:text-4xl lg:text-5xl font-black text-[var(--text-main)] tracking-tight">
            Interconnected Broadcast Signal Routing
          </h2>
          <p className="mt-3 text-xs sm:text-sm text-[var(--text-secondary)] leading-relaxed">
            Kashtrix eliminates metadata silos. A single media registration in Ingest automatically flows through MAM, QC, Newsroom MOS, 3D CG, Master Playout, StreamOps OTT, and OSS/BSS billing reconciliation.
          </p>
        </div>

        {/* Interactive Signal Flow Pipeline */}
        <BroadcastSignalFlow />
      </section>

      {/* 3. Hardware & Codec Certification Wall */}
      <section className="py-16 bg-[var(--bg-surface)] border-y border-[var(--border-subtle)] transition-colors duration-200">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="text-center max-w-2xl mx-auto mb-10">
            <span className="text-xs font-mono font-bold uppercase tracking-wider text-[var(--accent)]">
              CERTIFIED HARDWARE & PROTOCOL INTEROPERABILITY
            </span>
            <h3 className="text-2xl sm:text-3xl font-black text-[var(--text-main)] mt-1">
              Tested on Professional Broadcast Standards
            </h3>
          </div>

          <div className="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-6 gap-3.5">
            {[
              {
                partner: 'Blackmagic Design',
                models: 'DeckLink 8K Pro • Quad 2 • Duo 2 • UltraStudio',
                spec: '12G / 3G-SDI Baseband'
              },
              {
                partner: 'AJA Video Systems',
                models: 'Kona 5 • Kona 4 • Corvid 44 / 88',
                spec: 'Multi-Stream Baseband & ST 2110'
              },
              {
                partner: 'Matrox Video',
                models: 'DSX LE4 • X.mio5 IP / SDI',
                spec: 'SMPTE ST 2110 & 12G-SDI'
              },
              {
                partner: 'SMPTE ST 2110',
                models: 'ST 2110-20 (Video) • -30 (Audio) • -40 (ANC)',
                spec: 'Uncompressed 100GbE IP Fabric'
              },
              {
                partner: 'NDI® 6',
                models: 'NDI High Profile & NDI HX3 10-Bit HDR',
                spec: 'Zero-Config Local IP Production'
              },
              {
                partner: 'SCTE-35 & MOS 2.8.4',
                models: 'Digital Splice Cue Triggers & Newsroom Protocol',
                spec: 'Automated Downstream SSAI & AP/Avid'
              }
            ].map((hw, idx) => (
              <div
                key={idx}
                className="p-4 rounded-xl bg-[var(--bg-card)] border border-[var(--border-subtle)] shadow-xs hover:border-[var(--accent)] transition-all"
              >
                <div className="text-[11px] font-mono font-bold text-[var(--accent)] uppercase">
                  {hw.partner}
                </div>
                <div className="text-xs font-bold text-[var(--text-main)] mt-1">
                  {hw.spec}
                </div>
                <p className="text-[10px] text-[var(--text-muted)] mt-1 leading-snug">
                  {hw.models}
                </p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* 4. Complete Software Suite Directory (Table of all 17 Applications with Dedicated Single Pages) */}
      <SoftwareProductsDirectory />

      {/* 5. Mission-Critical High Availability & Engineering Redundancy */}
      <section className="py-20 max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="rounded-2xl p-6 sm:p-12 bg-[var(--bg-card)] border border-[var(--border-subtle)] shadow-xl relative overflow-hidden">
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-10 items-center">
            <div>
              <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-[var(--bg-surface)] border border-[var(--border-subtle)] text-xs font-bold text-emerald-600 dark:text-emerald-400 mb-3 shadow-xs">
                <ShieldCheck className="w-4 h-4 text-emerald-500" />
                <span>UNINTERRUPTED 24/7 ON-AIR AVAILABILITY</span>
              </div>
              <h2 className="text-2xl sm:text-3xl lg:text-4xl font-black text-[var(--text-main)] tracking-tight leading-tight">
                Active 1+1 Mirror & N+M High Availability Failover
              </h2>
              <p className="mt-3 text-xs sm:text-sm text-[var(--text-secondary)] leading-relaxed">
                In linear television, dropping frames is unacceptable. Kashtrix features integrated HA controllers that synchronize channel playlists, clip positions, and graphics state across active and hot standby engines. If a node fails, the cluster executes a sub-second switchover without disturbing viewers.
              </p>

              <div className="mt-6 space-y-3.5">
                {[
                  {
                    title: 'SMPTE ST 2022-7 Hitless Redundancy',
                    desc: 'A/B dual network paths ensure zero packet loss across uncompressed 100GbE IP fabrics.'
                  },
                  {
                    title: 'Sub-Frame Hardware Switching',
                    desc: 'Smooth baseband video cutover synchronized to studio genlock and PTP IEEE 1588 timing.'
                  },
                  {
                    title: 'SCTE-35 & SCTE-104 Ad Splicing',
                    desc: 'Digital cue trigger insertion guarantees frame-accurate commercial replacement on digital feeds.'
                  }
                ].map((item, i) => (
                  <div key={i} className="flex items-start gap-2.5">
                    <CheckCircle2 className="w-4 h-4 text-emerald-500 shrink-0 mt-0.5" />
                    <div>
                      <h4 className="text-xs font-bold text-[var(--text-main)]">{item.title}</h4>
                      <p className="text-[11px] text-[var(--text-muted)] mt-0.5">{item.desc}</p>
                    </div>
                  </div>
                ))}
              </div>

              <div className="mt-8 flex flex-wrap gap-3">
                <Link
                  href="/products/ha-redundancy"
                  className="px-5 py-2.5 rounded-xl text-xs font-bold text-white bg-[var(--accent)] hover:bg-[var(--accent-dark)] transition-all shadow-xs flex items-center gap-1.5"
                >
                  <span>Explore HA Redundancy Architecture</span>
                  <ArrowRight className="w-3.5 h-3.5" />
                </Link>
                <Link
                  href="/architecture"
                  className="px-5 py-2.5 rounded-xl text-xs font-bold text-[var(--text-main)] bg-[var(--bg-surface)] hover:bg-[var(--bg-card-hover)] border border-[var(--border-subtle)] transition-all shadow-xs flex items-center gap-1.5"
                >
                  <span>View System Architecture</span>
                </Link>
              </div>
            </div>

            {/* Architecture Metrics Visualizer */}
            <div className="rounded-2xl p-5 bg-[var(--bg-surface)] border border-[var(--border-subtle)] space-y-4 shadow-sm font-mono text-xs">
              <div className="flex items-center justify-between pb-3 border-b border-[var(--border-subtle)]">
                <span className="font-bold text-[var(--text-main)]">HA CLUSTER TOPOLOGY</span>
                <span className="text-emerald-600 dark:text-emerald-400 font-bold flex items-center gap-1">
                  <span className="w-2 h-2 rounded-full bg-emerald-500 animate-ping" />
                  1+1 MIRROR ACTIVE
                </span>
              </div>

              <div className="space-y-3">
                <div className="p-3 rounded-xl bg-[var(--bg-card)] border border-[var(--border-subtle)]">
                  <div className="flex items-center justify-between text-[11px] font-bold text-[var(--text-main)] mb-1">
                    <span>PRIMARY NODE (MCR-TX-01)</span>
                    <span className="text-emerald-600 dark:text-emerald-400">TRANSMITTING PGM</span>
                  </div>
                  <div className="text-[10px] text-[var(--text-muted)]">
                    DirectX 11 • DeckLink 8K Pro • 2160p50 • SDI Out Locked
                  </div>
                  <div className="mt-2 w-full bg-[var(--bg-surface)] h-1.5 rounded-full overflow-hidden">
                    <div className="bg-emerald-500 h-full w-[100%]" />
                  </div>
                </div>

                <div className="p-3 rounded-xl bg-[var(--bg-card)] border border-[var(--border-subtle)]">
                  <div className="flex items-center justify-between text-[11px] font-bold text-[var(--text-main)] mb-1">
                    <span>BACKUP NODE (MCR-TX-02)</span>
                    <span className="text-amber-600 dark:text-amber-400">HOT STANDBY LOCKSTEP</span>
                  </div>
                  <div className="text-[10px] text-[var(--text-muted)]">
                    Synchronized Playlist • UDP Heartbeat 250ms • Bypass Auto-Trigger Ready
                  </div>
                  <div className="mt-2 w-full bg-[var(--bg-surface)] h-1.5 rounded-full overflow-hidden">
                    <div className="bg-amber-500 h-full w-[100%]" />
                  </div>
                </div>

                <div className="p-3 rounded-xl bg-[var(--bg-card)] border border-[var(--border-subtle)]">
                  <div className="flex items-center justify-between text-[11px] font-bold text-[var(--text-main)] mb-1">
                    <span>STREAMOPS OTT EDGE (OTT-TRANSCODER-01)</span>
                    <span className="text-[var(--accent)]">MULTI-CDN ACTIVE</span>
                  </div>
                  <div className="text-[10px] text-[var(--text-muted)]">
                    Low-Latency HLS • SCTE-35 SSAI • Akamai & Cloudflare Routing
                  </div>
                  <div className="mt-2 w-full bg-[var(--bg-surface)] h-1.5 rounded-full overflow-hidden">
                    <div className="bg-[var(--accent)] h-full w-[100%]" />
                  </div>
                </div>
              </div>

              <div className="pt-2 border-t border-[var(--border-subtle)] flex items-center justify-between text-[10px] text-[var(--text-muted)]">
                <span>HEARTBEAT FREQUENCY: 250 MS</span>
                <span>FAILOVER SWITCH TIME: &lt; 0.8 SECONDS</span>
              </div>
            </div>

          </div>
        </div>
      </section>

      {/* 6. Broadcaster Testimonials / Global Case Studies */}
      {testimonials.length > 0 && (
        <section className="py-20 bg-[var(--bg-surface)] border-y border-[var(--border-subtle)] transition-colors duration-200">
          <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
            <div className="text-center max-w-2xl mx-auto mb-12">
              <span className="text-xs font-mono font-bold uppercase tracking-wider text-[var(--accent)]">
                ENGINEER VALIDATION
              </span>
              <h2 className="text-3xl sm:text-4xl font-black text-[var(--text-main)] tracking-tight mt-1">
                Trusted by Broadcast Engineers Worldwide
              </h2>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
              {testimonials.map((t) => (
                <div
                  key={t.id}
                  className="p-6 rounded-2xl bg-[var(--bg-card)] border border-[var(--border-subtle)] shadow-xs flex flex-col justify-between"
                >
                  <div>
                    <Quote className="w-6 h-6 text-[var(--accent)] mb-3 opacity-60" />
                    <p className="text-xs sm:text-sm text-[var(--text-secondary)] leading-relaxed italic">
                      "{t.quote}"
                    </p>
                  </div>

                  <div className="mt-6 pt-4 border-t border-[var(--border-subtle)]">
                    <div className="font-bold text-xs text-[var(--text-main)]">{t.author}</div>
                    <div className="text-[11px] text-[var(--accent)] font-semibold">{t.role}</div>
                    <div className="text-[10px] text-[var(--text-muted)] font-mono">{t.station}</div>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </section>
      )}

      {/* 7. Engineering Evaluation License & Demo CTA */}
      <section className="py-20 max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="rounded-2xl p-8 sm:p-12 bg-gradient-to-br from-[var(--bg-card)] via-[var(--bg-surface)] to-[var(--bg-card)] border border-[var(--border-subtle)] shadow-xl text-center max-w-4xl mx-auto">
          <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-[var(--accent)]/10 text-xs font-bold text-[var(--accent)] mb-4">
            <Sparkles className="w-3.5 h-3.5" />
            <span>30-DAY ENGINEERING TRIAL AVAILABLE</span>
          </div>

          <h2 className="text-3xl sm:text-4xl font-black text-[var(--text-main)] tracking-tight">
            Ready to Deploy Kashtrix in Your Facility?
          </h2>

          <p className="mt-3 text-xs sm:text-sm text-[var(--text-secondary)] max-w-xl mx-auto leading-relaxed">
            Schedule a private engineering walkthrough with our solutions architects and test Kashtrix Enterprise Suite with your SDI/NDI feeds.
          </p>

          <div className="mt-8 flex flex-wrap items-center justify-center gap-3.5">
            <Link
              href="/contact"
              className="px-6 py-3 rounded-xl text-xs sm:text-sm font-bold text-white bg-[var(--accent)] hover:bg-[var(--accent-dark)] hover:shadow-md hover:shadow-[var(--accent)]/25 transition-all flex items-center gap-2 cursor-pointer"
            >
              <Sparkles className="w-4 h-4 text-white" />
              <span>Request 30-Day Evaluation License</span>
            </Link>

            <Link
              href="/products"
              className="px-6 py-3 rounded-xl text-xs sm:text-sm font-bold text-[var(--text-main)] bg-[var(--bg-card)] hover:bg-[var(--bg-card-hover)] border border-[var(--border-strong)] transition-all shadow-xs"
            >
              <span>Explore All 17 Software Products</span>
            </Link>
          </div>
        </div>
      </section>

      <Footer />
    </div>
  );
}
