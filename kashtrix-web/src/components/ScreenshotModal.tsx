'use client';

import React, { useState } from 'react';
import Image from 'next/image';
import { X, ZoomIn, ExternalLink } from 'lucide-react';

interface ScreenshotModalProps {
  src: string;
  alt: string;
  title?: string;
  caption?: string;
  className?: string;
}

export function ScreenshotCard({ src, alt, title, caption, className = '' }: ScreenshotModalProps) {
  const [isOpen, setIsOpen] = useState(false);

  return (
    <>
      <div
        onClick={() => setIsOpen(true)}
        className={`group relative rounded-2xl overflow-hidden bg-[var(--bg-card)] border border-[var(--border-subtle)] hover:border-[var(--border-strong)] transition-all duration-300 cursor-pointer shadow-lg hover:shadow-2xl ${className}`}
      >
        <div className="relative aspect-video w-full overflow-hidden bg-black/60">
          <img
            src={src}
            alt={alt}
            className="w-full h-full object-cover object-top transition-transform duration-500 group-hover:scale-[1.03]"
            loading="lazy"
          />
          <div className="absolute inset-0 bg-gradient-to-t from-[var(--bg-card)] via-transparent to-transparent opacity-60 group-hover:opacity-30 transition-opacity" />
          
          {/* Zoom Overlay Pill */}
          <div className="absolute bottom-3 right-3 flex items-center gap-1.5 px-3 py-1.5 rounded-full bg-black/75 backdrop-blur-md border border-white/20 text-xs font-medium text-white opacity-0 group-hover:opacity-100 transition-opacity transform translate-y-1 group-hover:translate-y-0">
            <ZoomIn className="w-3.5 h-3.5 text-[var(--accent-light)]" />
            <span>Enlarge 4K</span>
          </div>
        </div>

        {(title || caption) && (
          <div className="p-4 bg-[var(--bg-card)] border-t border-[var(--border-subtle)]">
            {title && <h4 className="text-sm font-bold text-[var(--text-main)] group-hover:text-[var(--accent)] transition-colors">{title}</h4>}
            {caption && <p className="text-xs text-[var(--text-muted)] mt-1 line-clamp-2">{caption}</p>}
          </div>
        )}
      </div>

      {/* Lightbox Modal */}
      {isOpen && (
        <div 
          className="fixed inset-0 z-50 flex items-center justify-center p-4 sm:p-6 lg:p-8 bg-black/85 backdrop-blur-md animate-in fade-in duration-200"
          onClick={() => setIsOpen(false)}
        >
          <div 
            className="relative max-w-6xl w-full bg-[var(--bg-card)] border border-[var(--border-strong)] rounded-2xl overflow-hidden shadow-2xl flex flex-col max-h-[92vh]"
            onClick={(e) => e.stopPropagation()}
          >
            {/* Modal Header */}
            <div className="flex items-center justify-between px-6 py-4 border-b border-[var(--border-subtle)] bg-[var(--bg-surface)]">
              <div>
                <h3 className="text-base font-bold text-[var(--text-main)]">{title || alt}</h3>
                {caption && <p className="text-xs text-[var(--text-muted)] mt-0.5">{caption}</p>}
              </div>
              <button
                onClick={() => setIsOpen(false)}
                className="p-1.5 rounded-lg bg-[var(--bg-card)] text-[var(--text-muted)] hover:text-[var(--text-main)] border border-[var(--border-subtle)] transition-colors cursor-pointer"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            {/* Image Viewer */}
            <div className="relative overflow-auto p-3 bg-black/80 flex items-center justify-center min-h-[400px]">
              <img
                src={src}
                alt={alt}
                className="max-h-[75vh] w-auto max-w-full rounded-lg object-contain shadow-2xl border border-[var(--border-subtle)]"
              />
            </div>
          </div>
        </div>
      )}
    </>
  );
}
