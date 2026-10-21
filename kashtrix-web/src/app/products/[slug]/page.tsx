import React from 'react';
import Link from 'next/link';
import { notFound } from 'next/navigation';
import { prisma } from '@/lib/prisma';
import { Navbar } from '@/components/Navbar';
import { Footer } from '@/components/Footer';
import { ScreenshotCard } from '@/components/ScreenshotModal';
import { Icon } from '@/components/Icon';
import { 
  CheckCircle2, 
  ArrowLeft, 
  ArrowRight, 
  Cpu, 
  Layers, 
  Radio, 
  ShieldCheck, 
  Sparkles, 
  Download, 
  Share2, 
  Server
} from 'lucide-react';

interface PageProps {
  params: Promise<{ slug: string }>;
}

export const revalidate = 0;

export default async function ProductDetailPage({ params }: PageProps) {
  const { slug } = await params;

  const product = await prisma.product.findUnique({
    where: { slug },
    include: {
      features: { orderBy: { order: 'asc' } },
      screenshots: { orderBy: { order: 'asc' } }
    }
  });

  if (!product) {
    notFound();
  }

  // Related products in same or complementary category
  const relatedProducts = await prisma.product.findMany({
    where: {
      slug: { not: slug }
    },
    take: 3,
    orderBy: { order: 'asc' }
  });

  // Parse technical specs JSON
  let specsObj: Record<string, string> = {};
  try {
    specsObj = JSON.parse(product.specs);
  } catch (e) {
    specsObj = {};
  }

  return (
    <div className="min-h-screen bg-[var(--bg-page)] text-[var(--text-main)] transition-colors duration-200">
      <Navbar />

      <main className="pt-28 pb-24">
        {/* Breadcrumb & Navigation */}
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 mb-6">
          <div className="flex items-center gap-2 text-xs text-[var(--text-muted)]">
            <Link href="/" className="hover:text-[var(--text-main)] transition-colors">
              Home
            </Link>
            <span>/</span>
            <Link href="/products" className="hover:text-[var(--text-main)] transition-colors">
              Products
            </Link>
            <span>/</span>
            <span className="text-[var(--accent)] font-semibold">{product.category}</span>
            <span>/</span>
            <span className="text-[var(--text-main)] font-medium">{product.name}</span>
          </div>
        </div>

        {/* Product Hero Section */}
        <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 mb-16">
          <div className="grid grid-cols-1 lg:grid-cols-12 gap-12 items-center">
            
            {/* Left Content Column */}
            <div className="lg:col-span-6">
              <div className="flex items-center gap-2.5 mb-4">
                <span className="px-3 py-1 rounded-md text-xs font-mono font-bold uppercase tracking-wider bg-[var(--accent)]/15 text-[var(--accent-light)] border border-[var(--accent)]/30">
                  {product.badge}
                </span>
                <span className="px-2.5 py-1 rounded-md text-xs font-semibold bg-[var(--bg-card)] text-[var(--text-secondary)] border border-[var(--border-subtle)]">
                  {product.category}
                </span>
              </div>

              <div className="flex items-center gap-3.5 mb-3">
                <div className="p-3 rounded-2xl bg-[var(--bg-card)] border border-[var(--border-strong)] text-[var(--accent)] shadow-sm">
                  <Icon name={product.icon} size={28} />
                </div>
                <h1 className="text-3xl sm:text-4xl lg:text-5xl font-black text-[var(--text-main)] tracking-tight leading-tight">
                  {product.name}
                </h1>
              </div>

              <p className="text-base sm:text-lg font-semibold text-[var(--accent)] mb-6">
                {product.tagline}
              </p>

              <p className="text-sm sm:text-base text-[var(--text-secondary)] leading-relaxed mb-8">
                {product.description}
              </p>

              <div className="flex flex-wrap items-center gap-4">
                <Link
                  href="/contact"
                  className="px-6 py-3.5 rounded-xl text-sm font-bold text-white bg-gradient-to-r from-[#7C3AED] via-[#8B5CF6] to-[#EC4899] hover:shadow-xl hover:shadow-[#7C3AED]/30 transition-all flex items-center gap-2"
                >
                  <Sparkles className="w-4 h-4 text-white" />
                  <span>Request Engineering Demo</span>
                </Link>

                <Link
                  href="/contact"
                  className="px-6 py-3.5 rounded-xl text-sm font-bold text-[var(--text-main)] bg-[var(--bg-card)] hover:bg-[var(--bg-card-hover)] border border-[var(--border-strong)] hover:border-[var(--accent)] transition-all flex items-center gap-2 shadow-sm"
                >
                  <Download className="w-4 h-4 text-[var(--text-muted)]" />
                  <span>Download Tech Specs</span>
                </Link>
              </div>
            </div>

            {/* Right Hero Screenshot Card */}
            <div className="lg:col-span-6">
              <div className="p-3 rounded-3xl bg-[var(--bg-card)] border border-[var(--border-strong)] shadow-2xl">
                <div className="flex items-center justify-between px-4 py-2.5 bg-[var(--bg-surface)] rounded-t-2xl border-b border-[var(--border-subtle)] mb-1">
                  <div className="flex items-center gap-2">
                    <span className="w-2.5 h-2.5 rounded-full bg-[#EF4444]" />
                    <span className="w-2.5 h-2.5 rounded-full bg-[#F59E0B]" />
                    <span className="w-2.5 h-2.5 rounded-full bg-[#10B981]" />
                    <span className="ml-2 text-[11px] font-mono text-[var(--text-muted)] truncate max-w-xs">
                      {product.name} • Master View
                    </span>
                  </div>
                  <div className="text-[10px] font-mono text-emerald-400 font-bold">
                    • ON-AIR READY
                  </div>
                </div>
                <ScreenshotCard
                  src={product.heroImage}
                  alt={product.name}
                  title={`${product.name} Interface`}
                  caption={product.summary}
                  className="rounded-t-none border-t-0"
                />
              </div>
            </div>

          </div>
        </section>

        {/* Feature Highlights Grid */}
        {product.features && product.features.length > 0 && (
          <section className="py-16 bg-[var(--bg-surface)] border-y border-[var(--border-subtle)]">
            <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
              <div className="text-center max-w-2xl mx-auto mb-12">
                <h2 className="text-2xl sm:text-3xl font-extrabold text-[var(--text-main)] tracking-tight">
                  Architectural Capabilities & Features
                </h2>
                <p className="mt-2 text-sm text-[var(--text-muted)]">
                  Designed for mission-critical operations with zero single points of failure.
                </p>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
                {product.features.map((feat) => (
                  <div
                    key={feat.id}
                    className="p-6 rounded-2xl bg-[var(--bg-card)] border border-[var(--border-subtle)] hover:border-[var(--border-strong)] transition-all flex flex-col justify-between shadow-sm hover:shadow-md"
                  >
                    <div>
                      <div className="p-3 rounded-xl bg-[var(--bg-surface)] border border-[var(--border-subtle)] text-[var(--accent)] inline-block mb-4">
                        <Icon name={feat.icon} size={22} />
                      </div>
                      <h3 className="text-base font-bold text-[var(--text-main)] mb-2">
                        {feat.title}
                      </h3>
                      <p className="text-xs text-[var(--text-muted)] leading-relaxed">
                        {feat.description}
                      </p>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </section>
        )}

        {/* Technical Specifications Table */}
        {Object.keys(specsObj).length > 0 && (
          <section className="py-16 max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
            <div className="max-w-4xl mx-auto">
              <div className="flex items-center gap-3 mb-6">
                <div className="p-2 rounded-xl bg-[var(--bg-card)] border border-[var(--border-subtle)] text-emerald-400">
                  <Cpu className="w-5 h-5" />
                </div>
                <div>
                  <h2 className="text-2xl font-bold text-[var(--text-main)]">Technical Specifications</h2>
                  <p className="text-xs text-[var(--text-muted)]">Hardware requirements, protocols, and encoding pipelines</p>
                </div>
              </div>

              <div className="rounded-2xl border border-[var(--border-subtle)] bg-[var(--bg-card)] overflow-hidden shadow-sm">
                <table className="w-full text-left text-xs">
                  <tbody>
                    {Object.entries(specsObj).map(([key, value], idx) => (
                      <tr
                        key={idx}
                        className={idx % 2 === 0 ? 'bg-[var(--bg-card)]' : 'bg-[var(--bg-surface)]'}
                      >
                        <td className="py-3.5 px-6 font-bold text-[var(--text-main)] w-1/3 border-b border-[var(--border-subtle)]">
                          {key}
                        </td>
                        <td className="py-3.5 px-6 text-[var(--text-secondary)] font-mono border-b border-[var(--border-subtle)]">
                          {value}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          </section>
        )}

        {/* Screenshot Gallery */}
        {product.screenshots && product.screenshots.length > 0 && (
          <section className="py-16 bg-[var(--bg-surface)] border-t border-[var(--border-subtle)]">
            <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
              <div className="flex items-center justify-between mb-8">
                <div>
                  <h2 className="text-2xl font-bold text-[var(--text-main)]">
                    Operational Gallery & Workspaces
                  </h2>
                  <p className="text-xs text-[var(--text-muted)] mt-1">
                    Click any view to inspect full broadcast resolution, layout panels & control telemetry.
                  </p>
                </div>
                <span className="text-xs font-mono text-[var(--text-muted)]">
                  {product.screenshots.length} Captures Available
                </span>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                {product.screenshots.map((shot) => (
                  <ScreenshotCard
                    key={shot.id}
                    src={shot.imagePath}
                    alt={shot.title}
                    title={shot.title}
                    caption={shot.caption}
                  />
                ))}
              </div>
            </div>
          </section>
        )}

        {/* Sibling Products in Suite */}
        <section className="py-16 max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 border-t border-[var(--border-subtle)]">
          <div className="flex items-center justify-between mb-8">
            <h2 className="text-xl font-bold text-[var(--text-main)]">
              Related Broadcast Modules in Kashtrix
            </h2>
            <Link
              href="/products"
              className="text-xs font-bold text-[var(--accent)] hover:underline flex items-center gap-1"
            >
              <span>View All Applications Matrix</span>
              <ArrowRight className="w-3.5 h-3.5" />
            </Link>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            {relatedProducts.map((rel) => (
              <Link
                key={rel.id}
                href={`/products/${rel.slug}`}
                className="p-5 rounded-2xl bg-[var(--bg-card)] border border-[var(--border-subtle)] hover:border-[var(--border-strong)] transition-all group shadow-sm"
              >
                <div className="flex items-center gap-3 mb-2">
                  <div className="p-2 rounded-xl bg-[var(--bg-surface)] border border-[var(--border-subtle)] text-[var(--accent)] group-hover:scale-105 transition-transform">
                    <Icon name={rel.icon} size={18} />
                  </div>
                  <div>
                    <h4 className="text-sm font-bold text-[var(--text-main)] group-hover:text-[var(--accent)] transition-colors">
                      {rel.name}
                    </h4>
                    <span className="text-[10px] text-[var(--text-muted)] uppercase font-semibold">
                      {rel.category}
                    </span>
                  </div>
                </div>
                <p className="text-xs text-[var(--text-muted)] line-clamp-2 mt-2">
                  {rel.summary}
                </p>
              </Link>
            ))}
          </div>
        </section>

      </main>

      <Footer />
    </div>
  );
}
