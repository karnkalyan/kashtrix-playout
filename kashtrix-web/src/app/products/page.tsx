import React from 'react';
import { prisma } from '@/lib/prisma';
import { Navbar } from '@/components/Navbar';
import { Footer } from '@/components/Footer';
import { ProductsFilterClient } from './ProductsFilterClient';
import { Layers } from 'lucide-react';

export const revalidate = 0;

export default async function ProductsPage() {
  const products = await prisma.product.findMany({
    include: {
      features: {
        orderBy: { order: 'asc' },
        take: 3
      }
    },
    orderBy: { order: 'asc' }
  });

  return (
    <div className="min-h-screen bg-[var(--bg-page)] text-[var(--text-main)] transition-colors duration-200">
      <Navbar />

      <main className="pt-32 pb-24 max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        {/* Header */}
        <div className="text-center max-w-3xl mx-auto mb-12">
          <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-[var(--bg-card)] border border-[var(--border-subtle)] text-xs font-semibold text-[var(--accent)] mb-3 shadow-sm">
            <Layers className="w-3.5 h-3.5 text-emerald-400" />
            <span>MODULAR TELEVISION PRODUCTION SUITE</span>
          </div>
          <h1 className="text-4xl sm:text-5xl font-black text-[var(--text-main)] tracking-tight">
            Kashtrix Broadcast Applications Matrix
          </h1>
          <p className="mt-4 text-base text-[var(--text-muted)]">
            A complete suite of specialized, interoperable broadcast engines. Deploy individual standalone modules or operate as an end-to-end multi-channel network.
          </p>
        </div>

        {/* Filter & Product List */}
        <ProductsFilterClient products={products} />
      </main>

      <Footer />
    </div>
  );
}
