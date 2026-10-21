'use client';

import React, { useState } from 'react';
import { Navbar } from '@/components/Navbar';
import { Footer } from '@/components/Footer';
import { 
  Send, 
  CheckCircle2, 
  Tv, 
  Mail, 
  Phone, 
  MapPin, 
  Radio, 
  ShieldCheck, 
  Clock, 
  Sparkles 
} from 'lucide-react';

export default function ContactPage() {
  const [formData, setFormData] = useState({
    name: '',
    email: '',
    company: '',
    channelCount: '1-4 Channels',
    productInterest: 'Kashtrix Playout Engine',
    message: ''
  });
  const [loading, setLoading] = useState(false);
  const [submitted, setSubmitted] = useState(false);
  const [errorMsg, setErrorMsg] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setErrorMsg('');

    try {
      const res = await fetch('/api/inquiries', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(formData)
      });
      const data = await res.json();

      if (!res.ok) {
        throw new Error(data.error || 'Failed to submit inquiry.');
      }

      setSubmitted(true);
    } catch (err: any) {
      setErrorMsg(err.message || 'An error occurred while submitting.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-[var(--bg-page)] text-[var(--text-main)] transition-colors duration-200">
      <Navbar />

      <main className="pt-32 pb-24 max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        
        {/* Header */}
        <div className="text-center max-w-3xl mx-auto mb-16">
          <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-[var(--bg-card)] border border-[var(--border-subtle)] text-xs font-semibold text-[var(--accent)] mb-3 shadow-sm">
            <Radio className="w-3.5 h-3.5 text-emerald-400" />
            <span>SOLUTIONS ARCHITECTURE & SALES ENGINEERING</span>
          </div>
          <h1 className="text-4xl sm:text-5xl font-black text-[var(--text-main)] tracking-tight">
            Schedule a Broadcast Systems Demonstration
          </h1>
          <p className="mt-4 text-base text-[var(--text-muted)] leading-relaxed">
            Connect directly with Kashtrix broadcast engineers. We will prepare a customized demonstration tailored to your channel count, format standards (SDI, ST 2110, NDI 6), and newsroom workflow.
          </p>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-12 gap-12 items-start">
          
          {/* Left Contact & Facility Info */}
          <div className="lg:col-span-5 space-y-6">
            <div className="p-8 rounded-3xl bg-[var(--bg-card)] border border-[var(--border-subtle)] shadow-sm">
              <h3 className="text-lg font-bold text-[var(--text-main)] mb-6">
                Broadcast Engineering Centers
              </h3>

              <div className="space-y-6 text-sm">
                <div className="flex items-start gap-4">
                  <div className="p-2.5 rounded-xl bg-[var(--bg-surface)] border border-[var(--border-subtle)] text-[var(--accent)]">
                    <MapPin className="w-5 h-5" />
                  </div>
                  <div>
                    <h4 className="font-bold text-[var(--text-main)]">Global Headquarters</h4>
                    <p className="text-xs text-[var(--text-muted)] mt-1">
                      Kashtrix Broadcast Systems Engineering<br />
                      Broadcast Technology Tower • Worldwide Deployment
                    </p>
                  </div>
                </div>

                <div className="flex items-start gap-4">
                  <div className="p-2.5 rounded-xl bg-[var(--bg-surface)] border border-[var(--border-subtle)] text-[var(--accent)]">
                    <Mail className="w-5 h-5" />
                  </div>
                  <div>
                    <h4 className="font-bold text-[var(--text-main)]">Direct Technical Email</h4>
                    <p className="text-xs font-mono text-[var(--text-muted)] mt-1">
                      engineering@kashtrix.tv<br />
                      support@kashtrix.tv
                    </p>
                  </div>
                </div>

                <div className="flex items-start gap-4">
                  <div className="p-2.5 rounded-xl bg-[var(--bg-surface)] border border-[var(--border-subtle)] text-[var(--accent)]">
                    <Phone className="w-5 h-5" />
                  </div>
                  <div>
                    <h4 className="font-bold text-[var(--text-main)]">24/7 Broadcast NOC Hotline</h4>
                    <p className="text-xs font-mono text-[var(--text-muted)] mt-1">
                      +1 (800) 555-KASH (Tier-1 SLA Priority)
                    </p>
                  </div>
                </div>
              </div>
            </div>

            {/* SLA Guarantee Box */}
            <div className="p-6 rounded-2xl bg-[var(--bg-card)] border border-[var(--border-subtle)] shadow-sm space-y-3">
              <div className="flex items-center gap-2 text-emerald-400 font-bold text-xs uppercase tracking-wider">
                <ShieldCheck className="w-4 h-4" />
                <span>Enterprise SLA Guarantee</span>
              </div>
              <p className="text-xs text-[var(--text-muted)] leading-relaxed">
                All production deployments include 24/7/365 active monitoring, hot standby mirror clustering, sub-second failover, and remote field engineer support.
              </p>
            </div>
          </div>

          {/* Right Form */}
          <div className="lg:col-span-7">
            <div className="p-8 sm:p-10 rounded-3xl bg-[var(--bg-card)] border border-[var(--border-strong)] shadow-2xl relative">
              {submitted ? (
                <div className="text-center py-12 space-y-4">
                  <div className="w-16 h-16 rounded-full bg-emerald-500/10 border border-emerald-500/30 flex items-center justify-center text-emerald-400 mx-auto">
                    <CheckCircle2 className="w-8 h-8" />
                  </div>
                  <h3 className="text-2xl font-black text-[var(--text-main)]">Demonstration Request Confirmed</h3>
                  <p className="text-sm text-[var(--text-muted)] max-w-md mx-auto">
                    Thank you, {formData.name}. Our broadcast solutions architects have received your request and will contact you within 2 business hours.
                  </p>
                  <button
                    onClick={() => {
                      setSubmitted(false);
                      setFormData({
                        name: '',
                        email: '',
                        company: '',
                        channelCount: '1-4 Channels',
                        productInterest: 'Kashtrix Playout Engine',
                        message: ''
                      });
                    }}
                    className="mt-4 px-6 py-2.5 rounded-xl bg-[var(--accent)] text-white text-xs font-semibold"
                  >
                    Submit Another Request
                  </button>
                </div>
              ) : (
                <form onSubmit={handleSubmit} className="space-y-5">
                  <h3 className="text-xl font-black text-[var(--text-main)]">
                    Book an Engineering Walkthrough
                  </h3>

                  {errorMsg && (
                    <div className="p-3.5 rounded-xl bg-red-500/10 border border-red-500/30 text-red-400 text-xs">
                      {errorMsg}
                    </div>
                  )}

                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                    <div>
                      <label className="block text-xs font-semibold text-[var(--text-secondary)] mb-1.5">
                        Your Full Name *
                      </label>
                      <input
                        type="text"
                        required
                        value={formData.name}
                        onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                        placeholder="Johnathan Davis"
                        className="w-full px-4 py-2.5 rounded-xl bg-[var(--bg-surface)] border border-[var(--border-subtle)] focus:border-[var(--accent)] text-xs text-[var(--text-main)] focus:outline-none transition-colors"
                      />
                    </div>

                    <div>
                      <label className="block text-xs font-semibold text-[var(--text-secondary)] mb-1.5">
                        Work Email Address *
                      </label>
                      <input
                        type="email"
                        required
                        value={formData.email}
                        onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                        placeholder="j.davis@network.tv"
                        className="w-full px-4 py-2.5 rounded-xl bg-[var(--bg-surface)] border border-[var(--border-subtle)] focus:border-[var(--accent)] text-xs text-[var(--text-main)] focus:outline-none transition-colors"
                      />
                    </div>
                  </div>

                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                    <div>
                      <label className="block text-xs font-semibold text-[var(--text-secondary)] mb-1.5">
                        Network / Broadcast Organization
                      </label>
                      <input
                        type="text"
                        value={formData.company}
                        onChange={(e) => setFormData({ ...formData, company: e.target.value })}
                        placeholder="Metro 24 News Network"
                        className="w-full px-4 py-2.5 rounded-xl bg-[var(--bg-surface)] border border-[var(--border-subtle)] focus:border-[var(--accent)] text-xs text-[var(--text-main)] focus:outline-none transition-colors"
                      />
                    </div>

                    <div>
                      <label className="block text-xs font-semibold text-[var(--text-secondary)] mb-1.5">
                        Active Television Channels
                      </label>
                      <select
                        value={formData.channelCount}
                        onChange={(e) => setFormData({ ...formData, channelCount: e.target.value })}
                        className="w-full px-4 py-2.5 rounded-xl bg-[var(--bg-surface)] border border-[var(--border-subtle)] focus:border-[var(--accent)] text-xs text-[var(--text-main)] focus:outline-none transition-colors"
                      >
                        <option value="1-4 Channels">1 - 4 Channels</option>
                        <option value="5-12 Channels">5 - 12 Channels</option>
                        <option value="13-32 Channels">13 - 32 Channels</option>
                        <option value="32+ Enterprise Channels">32+ Enterprise Channels</option>
                      </select>
                    </div>
                  </div>

                  <div>
                    <label className="block text-xs font-semibold text-[var(--text-secondary)] mb-1.5">
                      Primary Application of Interest
                    </label>
                    <select
                      value={formData.productInterest}
                      onChange={(e) => setFormData({ ...formData, productInterest: e.target.value })}
                      className="w-full px-4 py-2.5 rounded-xl bg-[var(--bg-surface)] border border-[var(--border-subtle)] focus:border-[var(--accent)] text-xs text-[var(--text-main)] focus:outline-none transition-colors"
                    >
                      <option value="Kashtrix Playout Engine">Kashtrix Playout Engine (24/7 Channel Automation)</option>
                      <option value="Kashtrix Studio Live Switcher">Kashtrix Studio (12-Channel Live Switcher)</option>
                      <option value="Kashtrix CG Editor & Controller">Kashtrix 3D Motion Graphics & CG</option>
                      <option value="Kashtrix MAM & Ingest">Media Asset Management (MAM) & Ingest Server</option>
                      <option value="Kashtrix NRCS & Prompter">Newsroom Computer System (NRCS) & MOS</option>
                      <option value="Complete Enterprise Suite">Complete Unified Broadcast Suite</option>
                    </select>
                  </div>

                  <div>
                    <label className="block text-xs font-semibold text-[var(--text-secondary)] mb-1.5">
                      Technical Requirements / Project Notes
                    </label>
                    <textarea
                      rows={4}
                      value={formData.message}
                      onChange={(e) => setFormData({ ...formData, message: e.target.value })}
                      placeholder="Specify SDI vs ST 2110 IP requirements, newsroom MOS connectivity, or high-availability failover specifications..."
                      className="w-full px-4 py-2.5 rounded-xl bg-[var(--bg-surface)] border border-[var(--border-subtle)] focus:border-[var(--accent)] text-xs text-[var(--text-main)] focus:outline-none transition-colors"
                    />
                  </div>

                  <button
                    type="submit"
                    disabled={loading}
                    className="w-full py-3.5 rounded-xl bg-gradient-to-r from-[#7C3AED] via-[#8B5CF6] to-[#EC4899] text-white text-xs font-bold shadow-lg shadow-[#7C3AED]/30 hover:opacity-95 transition-all flex items-center justify-center gap-2 cursor-pointer"
                  >
                    {loading ? (
                      <span>Transmitting Inquiry to MCR...</span>
                    ) : (
                      <>
                        <Send className="w-4 h-4" />
                        <span>Submit Demonstration Request</span>
                      </>
                    )}
                  </button>
                </form>
              )}
            </div>
          </div>

        </div>

      </main>

      <Footer />
    </div>
  );
}
