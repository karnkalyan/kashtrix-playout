import React from 'react';
import Link from 'next/link';
import { Navbar } from '@/components/Navbar';
import { Footer } from '@/components/Footer';
import { ScreenshotCard } from '@/components/ScreenshotModal';
import { 
  Network, 
  Radio, 
  Server, 
  ShieldCheck, 
  Cpu, 
  Zap, 
  CheckCircle2, 
  Activity, 
  Layers, 
  ArrowRight
} from 'lucide-react';

export default function ArchitecturePage() {
  return (
    <div className="min-h-screen bg-[var(--bg-page)] text-[var(--text-main)] transition-colors duration-200">
      <Navbar />

      <main className="pt-32 pb-24 max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        
        {/* Header */}
        <div className="text-center max-w-3xl mx-auto mb-16">
          <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-[var(--bg-card)] border border-[var(--border-subtle)] text-xs font-semibold text-[var(--accent)] mb-3 shadow-sm">
            <Network className="w-3.5 h-3.5 text-emerald-400" />
            <span>ENTERPRISE BROADCAST INFRASTRUCTURE</span>
          </div>
          <h1 className="text-4xl sm:text-5xl font-black text-[var(--text-main)] tracking-tight">
            Kashtrix Broadcast Systems Architecture
          </h1>
          <p className="mt-4 text-base text-[var(--text-muted)] leading-relaxed">
            A unified broadcast ecosystem built for modern IP facilities, hybrid SDI environments, and cloud edge streaming. Built with SMPTE ST 2110, NDI 6, SCTE-35, and 1+1 high availability.
          </p>
        </div>

        {/* 4 Pillars Grid */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-20">
          {[
            {
              title: 'SMPTE ST 2110 & NMOS',
              badge: 'UNCOMPRESSED IP',
              desc: 'ST 2110-20 video, ST 2110-30 audio, ST 2110-40 metadata, and NMOS IS-04/IS-05 connection management.',
              icon: Network
            },
            {
              title: 'Hitless ST 2022-7',
              badge: 'SEAMLESS FAILOVER',
              desc: 'Dual redundant network packet paths (Path A & Path B) guaranteeing zero dropped frames upon fabric failure.',
              icon: ShieldCheck
            },
            {
              title: 'SCTE-35 Ad Splicing',
              badge: 'MONETIZATION',
              desc: 'Automated digital program cue injection into DVB-TS and SCTE-104 VANC for dynamic ad replacement.',
              icon: Zap
            },
            {
              title: 'MOS 2.8.4 Newsroom',
              badge: 'INTEROPERABILITY',
              desc: 'Bidirectional newsroom integration linking stories, video clips, and live 3D CG lower thirds seamlessly.',
              icon: Radio
            }
          ].map((pillar, i) => {
            const IconCmp = pillar.icon;
            return (
              <div
                key={i}
                className="p-6 rounded-2xl bg-[var(--bg-card)] border border-[var(--border-subtle)] hover:border-[var(--border-strong)] transition-all shadow-sm hover:shadow-md"
              >
                <div className="flex items-center justify-between mb-4">
                  <div className="p-2.5 rounded-xl bg-[var(--bg-surface)] border border-[var(--border-subtle)] text-[var(--accent)]">
                    <IconCmp className="w-5 h-5" />
                  </div>
                  <span className="text-[10px] font-mono font-bold px-2 py-0.5 rounded bg-[var(--bg-surface)] text-[var(--accent)] border border-[var(--border-subtle)]">
                    {pillar.badge}
                  </span>
                </div>
                <h3 className="text-base font-bold text-[var(--text-main)] mb-2">{pillar.title}</h3>
                <p className="text-xs text-[var(--text-muted)] leading-relaxed">{pillar.desc}</p>
              </div>
            );
          })}
        </div>

        {/* Real Screenshots Grid */}
        <div className="mb-20">
          <div className="text-center max-w-2xl mx-auto mb-10">
            <h2 className="text-2xl sm:text-3xl font-extrabold text-[var(--text-main)]">
              Hardware I/O, Redundancy & Signal Processing Telemetry
            </h2>
            <p className="text-xs text-[var(--text-muted)] mt-2">
              Actual control consoles captured from running Kashtrix broadcast production nodes.
            </p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
            <ScreenshotCard
              src="/assets/products/pro-io-st2110-scte.png"
              alt="Professional IO & ST 2110 Redundancy"
              title="SMPTE ST 2110 / ST 2022-7 / NMOS / SCTE Configuration"
              caption="Transmission settings for destination multicast IPs, PTP grandmaster clock lock, and SCTE-35 splicing."
            />
            <ScreenshotCard
              src="/assets/products/video-processor-grading.png"
              alt="Video Processor & Color Grading"
              title="Final Output Video Processing & Real-Time Grading"
              caption="Program color correction, gamma exposure, saturation, temperature, and RGB gains applied before transmission."
            />
          </div>
        </div>

        {/* Protocol & Latency Matrix */}
        <div className="rounded-3xl p-8 sm:p-10 bg-[var(--bg-card)] border border-[var(--border-subtle)] mb-16 shadow-sm">
          <h3 className="text-xl font-bold text-[var(--text-main)] mb-6">
            Supported Broadcast Interfaces & Communication Buses
          </h3>

          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 text-xs">
            <div className="p-4 rounded-xl bg-[var(--bg-surface)] border border-[var(--border-subtle)]">
              <span className="font-bold text-[var(--accent)] block text-sm mb-1">REST API (Port 9080)</span>
              <p className="text-[var(--text-muted)] mb-2">CRUD, rundowns, schedules, MAM assets, clip queries, and preset management.</p>
              <span className="font-mono text-emerald-400">HTTP / JSON • Swagger 3.0</span>
            </div>

            <div className="p-4 rounded-xl bg-[var(--bg-surface)] border border-[var(--border-subtle)]">
              <span className="font-bold text-[var(--accent)] block text-sm mb-1">WebSocket Bus (Port 9080)</span>
              <p className="text-[var(--text-muted)] mb-2">Real-time bi-directional telemetry: timecode, tally, VU meters, cue changes.</p>
              <span className="font-mono text-emerald-400">Sub-10ms Latency Push</span>
            </div>

            <div className="p-4 rounded-xl bg-[var(--bg-surface)] border border-[var(--border-subtle)]">
              <span className="font-bold text-[var(--accent)] block text-sm mb-1">TCP JSONL Socket (Port 9120)</span>
              <p className="text-[var(--text-muted)] mb-2">Persistent machine-to-machine channel controller orchestration.</p>
              <span className="font-mono text-emerald-400">Automated Heartbeat Keepalive</span>
            </div>

            <div className="p-4 rounded-xl bg-[var(--bg-surface)] border border-[var(--border-subtle)]">
              <span className="font-bold text-[var(--accent)] block text-sm mb-1">MOS Protocol 2.8.4</span>
              <p className="text-[var(--text-muted)] mb-2">roCreate, roStorySend, roElementAction newsroom computer transactions.</p>
              <span className="font-mono text-emerald-400">TCP Socket 10540/10541</span>
            </div>

            <div className="p-4 rounded-xl bg-[var(--bg-surface)] border border-[var(--border-subtle)]">
              <span className="font-bold text-[var(--accent)] block text-sm mb-1">UDP Low-Latency Multicast</span>
              <p className="text-[var(--text-muted)] mb-2">Emergency hold triggers, fast telemetry, and HA heartbeat packets.</p>
              <span className="font-mono text-emerald-400">Port 42420 / 42421</span>
            </div>

            <div className="p-4 rounded-xl bg-[var(--bg-surface)] border border-[var(--border-subtle)]">
              <span className="font-bold text-[var(--accent)] block text-sm mb-1">Model Context Protocol (MCP)</span>
              <p className="text-[var(--text-muted)] mb-2">Streamable HTTP interface for AI agents and automated broadcast copilots.</p>
              <span className="font-mono text-emerald-400">Typed JSON Tools & Resources</span>
            </div>
          </div>
        </div>

        {/* CTA */}
        <div className="text-center max-w-xl mx-auto">
          <Link
            href="/contact"
            className="inline-flex items-center gap-2 px-8 py-3.5 rounded-xl text-base font-bold text-white bg-gradient-to-r from-[#7C3AED] to-[#EC4899] hover:shadow-xl hover:shadow-[#7C3AED]/30 transition-all cursor-pointer"
          >
            <span>Request Full Engineering Integration Architecture Guide</span>
            <ArrowRight className="w-4 h-4" />
          </Link>
        </div>

      </main>

      <Footer />
    </div>
  );
}
