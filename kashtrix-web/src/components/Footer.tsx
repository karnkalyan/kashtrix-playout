'use client';

import React from 'react';
import Link from 'next/link';
import { Tv, Radio, Shield, Code, Heart, ArrowUpRight } from 'lucide-react';

export function Footer() {
  return (
    <footer className="bg-[var(--bg-surface)] border-t border-[var(--border-subtle)] pt-14 pb-12 text-[var(--text-muted)] transition-colors duration-200">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        
        {/* Top Status & Telemetry Bar */}
        <div className="p-4 rounded-2xl bg-[var(--bg-card)] border border-[var(--border-subtle)] mb-12 flex flex-col sm:flex-row items-center justify-between gap-4 shadow-xs">
          <div className="flex items-center gap-3">
            <span className="relative flex h-2.5 w-2.5">
              <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75" />
              <span className="relative inline-flex rounded-full h-2.5 w-2.5 bg-emerald-500" />
            </span>
            <div className="text-xs font-mono">
              <span className="text-[var(--text-main)] font-semibold">TRANSMISSION STATUS:</span>{' '}
              <span className="text-emerald-600 dark:text-emerald-400 font-bold">ALL 17 BROADCAST ENGINES OPERATIONAL • 100% HEALTHY</span>
            </div>
          </div>
          <div className="flex flex-wrap items-center gap-2.5 text-xs font-mono text-[var(--text-muted)]">
            <span>SMPTE ST 2110-20/30/40</span>
            <span className="text-[var(--border-strong)]">•</span>
            <span>NDI 6 HDR</span>
            <span className="text-[var(--border-strong)]">•</span>
            <span>MOS 2.8.4</span>
            <span className="text-[var(--border-strong)]">•</span>
            <span>SCTE-35 TS</span>
            <span className="text-[var(--border-strong)]">•</span>
            <span>STREAMOPS OTT</span>
          </div>
        </div>

        {/* 5-Column Directory */}
        <div className="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-5 gap-8 mb-12">
          
          {/* Brand Info */}
          <div className="col-span-2">
            <Link href="/" className="flex items-center gap-2.5 mb-3 group">
              <div className="p-2 rounded-xl bg-[var(--accent)] text-white shadow-xs">
                <Tv className="w-4 h-4 text-white" />
              </div>
              <span className="text-lg font-black tracking-tight text-[var(--text-main)]">
                KASHTRIX
              </span>
            </Link>
            <p className="text-xs sm:text-sm text-[var(--text-secondary)] max-w-sm mb-4 leading-relaxed">
              Enterprise Television Playout, Live Production Switcher, 3D Character Generator, Newsroom MOS, StreamOps OTT, and Kashtrix OSS/BSS Billing Suite.
            </p>
            <div className="text-[11px] text-[var(--text-muted)] font-mono">
              Engineered for Windows 11 / Server • DirectX 11 GPU Accelerated Pipeline
            </div>
          </div>

          {/* Core Playout & Switcher */}
          <div>
            <h4 className="text-xs font-bold uppercase tracking-wider text-[var(--text-main)] mb-3">
              Playout & Studio
            </h4>
            <ul className="space-y-2 text-xs">
              <li>
                <Link href="/products/playout" className="hover:text-[var(--accent)] transition-colors">
                  Kashtrix Playout Engine
                </Link>
              </li>
              <li>
                <Link href="/products/studio" className="hover:text-[var(--accent)] transition-colors">
                  Kashtrix Studio Switcher
                </Link>
              </li>
              <li>
                <Link href="/products/scheduler" className="hover:text-[var(--accent)] transition-colors">
                  Scheduler & Traffic Master
                </Link>
              </li>
              <li>
                <Link href="/products/channel-controller" className="hover:text-[var(--accent)] transition-colors">
                  Master Channel Controller
                </Link>
              </li>
              <li>
                <Link href="/products/multiview" className="hover:text-[var(--accent)] transition-colors">
                  Multiview & QC Wall
                </Link>
              </li>
            </ul>
          </div>

          {/* Graphics, Newsroom & Streaming */}
          <div>
            <h4 className="text-xs font-bold uppercase tracking-wider text-[var(--text-main)] mb-3">
              Graphics & Cloud
            </h4>
            <ul className="space-y-2 text-xs">
              <li>
                <Link href="/products/cg-editor" className="hover:text-[var(--accent)] transition-colors">
                  Kashtrix 3D CG Editor
                </Link>
              </li>
              <li>
                <Link href="/products/cg-controller" className="hover:text-[var(--accent)] transition-colors">
                  Kashtrix CG Controller
                </Link>
              </li>
              <li>
                <Link href="/products/nrcs-web" className="hover:text-[var(--accent)] transition-colors font-semibold text-[var(--accent)]">
                  Kashtrix NRCS Web
                </Link>
              </li>
              <li>
                <Link href="/products/streamops" className="hover:text-[var(--accent)] transition-colors font-semibold text-[var(--accent)]">
                  StreamOps OTT
                </Link>
              </li>
              <li>
                <Link href="/products/oss-bss" className="hover:text-[var(--accent)] transition-colors font-semibold text-[var(--accent)]">
                  Kashtrix OSS / BSS
                </Link>
              </li>
            </ul>
          </div>

          {/* Enterprise & Support */}
          <div>
            <h4 className="text-xs font-bold uppercase tracking-wider text-[var(--text-main)] mb-3">
              Operations & MCR
            </h4>
            <ul className="space-y-2 text-xs">
              <li>
                <Link href="/architecture" className="hover:text-[var(--accent)] transition-colors">
                  System Architecture
                </Link>
              </li>
              <li>
                <Link href="/admin" className="hover:text-[var(--accent)] transition-colors flex items-center gap-1 font-semibold text-emerald-600 dark:text-emerald-400">
                  <span>MCR CMS Admin</span>
                  <ArrowUpRight className="w-3 h-3" />
                </Link>
              </li>
              <li>
                <Link href="/contact" className="hover:text-[var(--accent)] transition-colors">
                  Request 30-Day Evaluation
                </Link>
              </li>
              <li>
                <Link href="/products" className="hover:text-[var(--accent)] transition-colors">
                  All 17 Products Index
                </Link>
              </li>
            </ul>
          </div>

        </div>

        {/* Bottom Legal & Copyright Bar */}
        <div className="pt-8 border-t border-[var(--border-subtle)] flex flex-col sm:flex-row items-center justify-between gap-4 text-xs">
          <div className="flex items-center gap-2">
            <span>© {new Date().getFullYear()} Kashtrix Broadcast Systems. All rights reserved.</span>
          </div>

          <div className="flex items-center gap-6 text-[11px]">
            <span>ST 2110 / SDI / NDI Certified</span>
            <span className="text-[var(--border-strong)]">•</span>
            <span>DirectX 11 Hardware Rendered</span>
            <span className="text-[var(--border-strong)]">•</span>
            <span>MOS 2.8.4 Newsroom Standard</span>
          </div>
        </div>

      </div>
    </footer>
  );
}
