'use client';

import React from 'react';
import Link from 'next/link';
import { motion } from 'framer-motion';
import { ArrowRight, CheckCircle2, Maximize2 } from 'lucide-react';
import { Icon } from './Icon';

interface ProductCardProps {
  product: {
    slug: string;
    name: string;
    tagline: string;
    category: string;
    badge: string;
    heroImage: string;
    summary: string;
    icon: string;
    accentColor: string;
    features?: { title: string }[];
  };
}

export function ProductCard({ product }: ProductCardProps) {
  return (
    <motion.div
      whileHover={{ y: -4, transition: { duration: 0.2 } }}
      className="group rounded-3xl bg-[var(--bg-card)] border border-[var(--border-subtle)] hover:border-[var(--border-strong)] transition-all duration-300 overflow-hidden flex flex-col shadow-md hover:shadow-xl relative"
    >
      {/* Top subtle highlight */}
      <div className="absolute inset-x-0 top-0 h-[1px] bg-gradient-to-r from-transparent via-[var(--border-strong)] to-transparent pointer-events-none" />

      {/* Screenshot Preview with Aspect Video */}
      <div className="relative aspect-video w-full overflow-hidden bg-black/60">
        <img
          src={product.heroImage}
          alt={product.name}
          className="w-full h-full object-cover object-top transition-transform duration-500 group-hover:scale-105"
          loading="lazy"
        />
        <div className="absolute inset-0 bg-gradient-to-t from-[var(--bg-card)] via-transparent to-transparent opacity-70" />

        {/* Badges on Top */}
        <div className="absolute top-3 left-3 flex items-center gap-2">
          <span className="px-2.5 py-1 rounded-lg text-[10px] font-mono font-bold uppercase tracking-wider bg-black/75 backdrop-blur-md text-white border border-white/20 shadow-md">
            {product.badge}
          </span>
          <span className="px-2 py-0.5 rounded-md text-[10px] font-semibold bg-[var(--bg-surface)]/85 backdrop-blur-md text-[var(--text-secondary)] border border-[var(--border-subtle)]">
            {product.category}
          </span>
        </div>
      </div>

      {/* Card Content */}
      <div className="p-6 flex-1 flex flex-col justify-between space-y-4">
        <div>
          <div className="flex items-center gap-3 mb-2">
            <div className="p-2.5 rounded-xl bg-[var(--bg-surface)] border border-[var(--border-subtle)] text-[var(--accent)] group-hover:bg-[var(--accent)] group-hover:text-white transition-all shrink-0">
              <Icon name={product.icon} size={18} />
            </div>
            <div>
              <h3 className="text-base font-black text-[var(--text-main)] group-hover:text-[var(--accent)] transition-colors">
                {product.name}
              </h3>
              <p className="text-xs font-medium text-[var(--text-secondary)] line-clamp-1">
                {product.tagline}
              </p>
            </div>
          </div>

          <p className="text-xs text-[var(--text-muted)] line-clamp-2 leading-relaxed mt-2 mb-4">
            {product.summary}
          </p>

          {/* Feature Highlights Checklist */}
          {product.features && product.features.length > 0 && (
            <div className="space-y-2 pt-3 border-t border-[var(--border-subtle)]">
              {product.features.slice(0, 3).map((f, i) => (
                <div key={i} className="flex items-center gap-2 text-xs text-[var(--text-secondary)]">
                  <CheckCircle2 className="w-3.5 h-3.5 text-emerald-400 shrink-0" />
                  <span className="truncate">{f.title}</span>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Action Link */}
        <Link
          href={`/products/${product.slug}`}
          className="w-full py-2.5 px-4 rounded-xl text-xs font-bold text-center bg-[var(--bg-surface)] hover:bg-[var(--accent)] text-[var(--text-main)] hover:text-white border border-[var(--border-subtle)] hover:border-[var(--accent)] transition-all flex items-center justify-center gap-2 group/btn cursor-pointer shadow-sm"
        >
          <span>View Specifications & Media</span>
          <ArrowRight className="w-3.5 h-3.5 group-hover/btn:translate-x-1 transition-transform" />
        </Link>
      </div>
    </motion.div>
  );
}
