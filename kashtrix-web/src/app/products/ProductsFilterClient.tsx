'use client';

import React, { useState, useMemo } from 'react';
import { ProductCard } from '@/components/ProductCard';
import { Search, Filter } from 'lucide-react';

interface ProductItem {
  id: number;
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
}

interface FilterProps {
  products: ProductItem[];
}

const CATEGORIES = [
  'All Applications',
  'Core Playout',
  'Live Production',
  'Graphics & CG',
  'Newsroom & MOS',
  'Streaming & OTT',
  'Business & Telecom',
  'Asset Management',
  'Monitoring & I/O'
];

export function ProductsFilterClient({ products }: FilterProps) {
  const [selectedCategory, setSelectedCategory] = useState('All Applications');
  const [searchQuery, setSearchQuery] = useState('');

  const filteredProducts = useMemo(() => {
    return products.filter((prod) => {
      const matchesCategory =
        selectedCategory === 'All Applications' || prod.category === selectedCategory;
      const matchesSearch =
        prod.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
        prod.tagline.toLowerCase().includes(searchQuery.toLowerCase()) ||
        prod.summary.toLowerCase().includes(searchQuery.toLowerCase());
      return matchesCategory && matchesSearch;
    });
  }, [products, selectedCategory, searchQuery]);

  return (
    <div>
      {/* Controls Bar: Search & Category Pills */}
      <div className="flex flex-col md:flex-row items-center justify-between gap-4 mb-10 pb-6 border-b border-[var(--border-subtle)]">
        {/* Category Pills */}
        <div className="flex flex-wrap items-center gap-2">
          {CATEGORIES.map((cat) => (
            <button
              key={cat}
              onClick={() => setSelectedCategory(cat)}
              className={`px-3.5 py-1.5 rounded-xl text-xs font-bold transition-all cursor-pointer ${
                selectedCategory === cat
                  ? 'bg-[var(--accent)] text-white shadow-md shadow-[var(--accent)]/25'
                  : 'bg-[var(--bg-card)] text-[var(--text-secondary)] hover:text-[var(--text-main)] hover:bg-[var(--bg-card-hover)] border border-[var(--border-subtle)]'
              }`}
            >
              {cat}
            </button>
          ))}
        </div>

        {/* Search Input */}
        <div className="relative w-full md:w-72">
          <Search className="w-4 h-4 text-[var(--text-muted)] absolute left-3.5 top-1/2 -translate-y-1/2" />
          <input
            type="text"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            placeholder="Search applications..."
            className="w-full pl-9 pr-4 py-2 rounded-xl bg-[var(--bg-card)] border border-[var(--border-subtle)] focus:border-[var(--accent)] text-xs text-[var(--text-main)] placeholder-[var(--text-muted)] focus:outline-none transition-colors"
          />
        </div>
      </div>

      {/* Results Count */}
      <div className="mb-6 flex items-center justify-between text-xs text-[var(--text-muted)] font-mono">
        <span>
          Showing <span className="font-bold text-[var(--text-main)]">{filteredProducts.length}</span> broadcast modules
        </span>
        {selectedCategory !== 'All Applications' && (
          <span className="text-[var(--accent)] font-semibold">Filtered by: {selectedCategory}</span>
        )}
      </div>

      {/* Grid of Products */}
      {filteredProducts.length > 0 ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {filteredProducts.map((prod) => (
            <ProductCard key={prod.id} product={prod} />
          ))}
        </div>
      ) : (
        <div className="text-center py-16 bg-[var(--bg-card)] rounded-3xl border border-[var(--border-subtle)]">
          <p className="text-sm text-[var(--text-muted)]">No applications match the search criteria.</p>
          <button
            onClick={() => {
              setSelectedCategory('All Applications');
              setSearchQuery('');
            }}
            className="mt-4 px-4 py-2 rounded-xl bg-[var(--accent)] text-white text-xs font-bold"
          >
            Clear Filters
          </button>
        </div>
      )}
    </div>
  );
}
