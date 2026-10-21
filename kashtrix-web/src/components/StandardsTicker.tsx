import React from 'react';
import { Zap } from 'lucide-react';

const STANDARDS = [
  'SMPTE ST 2110-20 / 30 / 40',
  'SMPTE ST 2022-7 Hitless Redundancy',
  'Blackmagic DeckLink 12G-SDI',
  'Matrox DSX LE / X.mio',
  'AJA Kona / Corvid Multi-Channel',
  'NDI 6 with 16-Bit HDR Pipeline',
  'SCTE-35 TS Splice & SCTE-104 VANC',
  'MOS Protocol 2.8.4 Newsroom',
  'EBU R128 / ITU-R BS.1770-4 Loudness',
  'DVB SI/PSI (PAT, PMT, SDT, NIT, TDT)',
  'NVIDIA NVENC H.264 / HEVC Hardware Encode',
  'XDCAM HD 422 50Mbps & Apple ProRes'
];

export function StandardsTicker() {
  return (
    <div className="border-y border-[var(--border-subtle)] bg-[var(--bg-surface)] py-3 overflow-hidden">
      <div className="max-w-7xl mx-auto px-4 flex items-center">
        <div className="hidden md:flex items-center gap-2 pr-6 border-r border-[var(--border-subtle)] shrink-0 text-xs font-mono font-bold text-[var(--accent)]">
          <Zap className="w-3.5 h-3.5 text-emerald-400" />
          <span>BROADCAST STANDARDS</span>
        </div>
        
        <div className="flex overflow-hidden space-x-8 pl-6 select-none font-mono text-xs text-[var(--text-muted)]">
          <div className="flex space-x-8 shrink-0 animate-marquee">
            {STANDARDS.map((std, i) => (
              <span key={i} className="flex items-center gap-2 shrink-0">
                <span className="w-1.5 h-1.5 rounded-full bg-emerald-400" />
                <span className="text-[var(--text-secondary)] font-medium">{std}</span>
              </span>
            ))}
          </div>
          <div className="flex space-x-8 shrink-0 animate-marquee" aria-hidden="true">
            {STANDARDS.map((std, i) => (
              <span key={`dup-${i}`} className="flex items-center gap-2 shrink-0">
                <span className="w-1.5 h-1.5 rounded-full bg-emerald-400" />
                <span className="text-[var(--text-secondary)] font-medium">{std}</span>
              </span>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
