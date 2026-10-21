'use client';

import React, { useState, useEffect } from 'react';
import Link from 'next/link';
import { useTheme } from '@/components/ThemeProvider';
import { 
  Tv, 
  Layers, 
  Radio, 
  Image as ImageIcon, 
  Mail, 
  Settings, 
  Plus, 
  Search, 
  Edit, 
  Trash2, 
  Save, 
  X, 
  CheckCircle2, 
  AlertCircle, 
  ArrowLeft, 
  RefreshCw, 
  ExternalLink, 
  Sun, 
  Moon, 
  Database, 
  ShieldCheck, 
  Activity, 
  BarChart3, 
  Server, 
  Sliders, 
  Eye, 
  ChevronRight,
  Sparkles,
  Command,
  LayoutDashboard,
  Video,
  FolderKanban,
  Cpu,
  Clock,
  ArrowUpRight,
  TrendingUp,
  Inbox,
  Filter,
  Check,
  Copy,
  Maximize2,
  MoreVertical,
  Volume2
} from 'lucide-react';
import { Icon } from '@/components/Icon';

interface ProductItem {
  id: number;
  slug: string;
  name: string;
  tagline: string;
  category: string;
  badge: string;
  heroImage: string;
  summary: string;
  description: string;
  icon: string;
  accentColor: string;
  specs: string;
  order: number;
  features: { id: number; title: string; description: string; icon: string; order: number }[];
  screenshots: { id: number; title: string; caption: string; imagePath: string; category: string }[];
}

interface InquiryItem {
  id: number;
  name: string;
  email: string;
  company: string | null;
  channelCount: string | null;
  productInterest: string | null;
  message: string;
  status: string;
  createdAt: string;
}

const AVAILABLE_IMAGES = [
  { label: 'Playout Master Rundown', path: '/assets/products/playout-master-rundown.png', category: 'Playout' },
  { label: 'Playout Preview Bus', path: '/assets/products/playout-preview-bus.png', category: 'Playout' },
  { label: 'Playout Media Explorer', path: '/assets/products/playout-media-explorer.png', category: 'Playout' },
  { label: 'Playout Web Client', path: '/assets/products/playout-web-client.png', category: 'Playout' },
  { label: 'Studio Switcher Full Console', path: '/assets/products/studio-switcher-full.png', category: 'Studio' },
  { label: 'Studio Switcher PVW/PGM Bus', path: '/assets/products/studio-switcher-pvw-pgm.png', category: 'Studio' },
  { label: 'CG Editor Main Workspace', path: '/assets/products/cg-editor-main.png', category: 'Graphics' },
  { label: 'CG Editor Compositor Canvas', path: '/assets/products/cg-editor-compositor.png', category: 'Graphics' },
  { label: 'CG Editor Inspector Panel', path: '/assets/products/cg-editor-inspector.png', category: 'Graphics' },
  { label: 'CG Editor Typography', path: '/assets/products/cg-editor-typography.png', category: 'Graphics' },
  { label: 'CG Editor Timeline Layers', path: '/assets/products/cg-editor-timeline.png', category: 'Graphics' },
  { label: 'CG Controller Main Surface', path: '/assets/products/cg-controller-main.png', category: 'Graphics' },
  { label: 'CG Controller Quick Edit Lower Third', path: '/assets/products/cg-controller-quickedit.png', category: 'Graphics' },
  { label: 'CG Controller Active Layers', path: '/assets/products/cg-controller-layers.png', category: 'Graphics' },
  { label: 'MAM Asset Catalog', path: '/assets/products/mam-catalog.png', category: 'MAM' },
  { label: 'MAM Storage Settings', path: '/assets/products/mam-storage-settings.png', category: 'MAM' },
  { label: 'Ingest Server Console', path: '/assets/products/ingest-server-main.png', category: 'Ingest' },
  { label: 'Multi-Source Live Input Grid', path: '/assets/products/input-engine-grid.png', category: 'Ingest' },
  { label: 'NRCS Enterprise Rundown', path: '/assets/products/nrcs-enterprise-overview.png', category: 'Newsroom' },
  { label: 'NRCS Newsroom Story Workflow', path: '/assets/products/nrcs-newsroom-workflow.png', category: 'Newsroom' },
  { label: 'Studio Teleprompter', path: '/assets/products/prompter-live-studio.png', category: 'Studio' },
  { label: 'Channel Controller Cluster', path: '/assets/products/channel-controller-cluster.png', category: 'Playout' },
  { label: 'Hardware Output Engine Baseband', path: '/assets/products/output-engine-baseband.png', category: 'Transmission' },
  { label: 'Multiview Wall & QC Engine', path: '/assets/products/multiview-wall-qc.png', category: 'Monitoring' },
  { label: 'Channel Confidence Wall', path: '/assets/products/multiview-monitoring.png', category: 'Monitoring' },
  { label: 'QC Compliance Probe', path: '/assets/products/qc-controller-compliance.png', category: 'Monitoring' },
  { label: 'Playlist Rundown Editor', path: '/assets/products/playlist-editor-rundown.png', category: 'Playout' },
  { label: '24/7 Channel Scheduler', path: '/assets/products/scheduler-24-7.png', category: 'Playout' },
  { label: 'HA Redundancy Cluster 1+1', path: '/assets/products/ha-redundancy-cluster.png', category: 'Transmission' },
  { label: 'Professional I/O & ST 2110 IP', path: '/assets/products/pro-io-st2110-scte.png', category: 'Transmission' },
  { label: 'Final Output Video Processor', path: '/assets/products/video-processor-grading.png', category: 'Transmission' },
  { label: 'API Gateway Unified Bus', path: '/assets/products/api-gateway-unified-bus.png', category: 'System' },
  { label: 'API Gateway Endpoints', path: '/assets/products/api-gateway-endpoints.png', category: 'System' },
  { label: 'Modular Suite Launcher', path: '/assets/products/suite-overview-launcher.png', category: 'System' }
];

export default function AdminDashboardPage() {
  const [activeSection, setActiveSection] = useState<'overview' | 'products' | 'media' | 'hero' | 'inquiries' | 'system'>('overview');
  const [products, setProducts] = useState<ProductItem[]>([]);
  const [inquiries, setInquiries] = useState<InquiryItem[]>([]);
  const [heroContent, setHeroContent] = useState<any>(null);
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedCategory, setSelectedCategory] = useState('ALL');
  const [productViewMode, setProductViewMode] = useState<'table' | 'grid'>('table');
  const [loading, setLoading] = useState(true);
  const [msg, setMsg] = useState<{ text: string; type: 'success' | 'error' } | null>(null);
  const [activeLightboxImg, setActiveLightboxImg] = useState<string | null>(null);
  const [copiedPath, setCopiedPath] = useState<string | null>(null);

  const { theme, toggleTheme } = useTheme();

  // Modals state
  const [editingProduct, setEditingProduct] = useState<ProductItem | null>(null);
  const [isCreatingProduct, setIsCreatingProduct] = useState(false);
  const [productForm, setProductForm] = useState({
    name: '',
    slug: '',
    tagline: '',
    category: 'Core Playout',
    badge: 'BROADCAST MODULE',
    heroImage: '/assets/products/playout-master-rundown.png',
    summary: '',
    description: '',
    icon: 'Tv',
    accentColor: '#8B5CF6',
    specs: '{}',
    order: 0
  });

  const [selectedProductForMedia, setSelectedProductForMedia] = useState<ProductItem | null>(null);
  const [newFeatureForm, setNewFeatureForm] = useState({ title: '', description: '', icon: 'CheckCircle' });
  const [newScreenshotForm, setNewScreenshotForm] = useState({
    title: '',
    caption: '',
    imagePath: AVAILABLE_IMAGES[0].path,
    category: 'Interface'
  });

  const fetchData = async () => {
    setLoading(true);
    try {
      const [prodRes, heroRes, inqRes] = await Promise.all([
        fetch('/api/admin/products'),
        fetch('/api/admin/hero'),
        fetch('/api/inquiries')
      ]);

      const prodData = await prodRes.json();
      const heroData = await heroRes.json();
      const inqData = await inqRes.json();

      if (prodData.products) setProducts(prodData.products);
      if (heroData.hero) setHeroContent(heroData.hero);
      if (inqData.inquiries) setInquiries(inqData.inquiries);
    } catch (e: any) {
      setMsg({ text: 'Error connecting to database: ' + e.message, type: 'error' });
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
  }, []);

  const showNotification = (text: string, type: 'success' | 'error' = 'success') => {
    setMsg({ text, type });
    setTimeout(() => setMsg(null), 4000);
  };

  const copyToClipboard = (path: string) => {
    navigator.clipboard.writeText(path);
    setCopiedPath(path);
    showNotification('Asset path copied to clipboard: ' + path);
    setTimeout(() => setCopiedPath(null), 2500);
  };

  // Save product
  const handleSaveProduct = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      if (editingProduct) {
        const res = await fetch('/api/admin/products', {
          method: 'PUT',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ id: editingProduct.id, ...productForm })
        });
        const resData = await res.json();
        if (!res.ok) throw new Error(resData.error);
        showNotification('Module updated successfully in MySQL!');
      } else {
        const res = await fetch('/api/admin/products', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(productForm)
        });
        const resData = await res.json();
        if (!res.ok) throw new Error(resData.error);
        showNotification('New broadcast module created in MySQL!');
      }

      setEditingProduct(null);
      setIsCreatingProduct(false);
      fetchData();
    } catch (err: any) {
      showNotification(err.message, 'error');
    }
  };

  // Delete product
  const handleDeleteProduct = async (id: number, name: string) => {
    if (!confirm(`Are you sure you want to delete "${name}" from the Kashtrix Broadcast Suite?`)) return;

    try {
      const res = await fetch(`/api/admin/products?id=${id}`, { method: 'DELETE' });
      if (!res.ok) throw new Error('Failed to delete module.');
      showNotification(`"${name}" removed from suite.`);
      fetchData();
    } catch (err: any) {
      showNotification(err.message, 'error');
    }
  };

  // Add Feature
  const handleAddFeature = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedProductForMedia) return;

    try {
      const res = await fetch('/api/admin/features', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          productId: selectedProductForMedia.id,
          ...newFeatureForm
        })
      });
      if (!res.ok) throw new Error('Failed to add feature');
      showNotification('Feature added to module!');
      setNewFeatureForm({ title: '', description: '', icon: 'CheckCircle' });
      fetchData();
      const updatedProdRes = await fetch('/api/admin/products');
      const updatedData = await updatedProdRes.json();
      const updated = updatedData.products.find((p: any) => p.id === selectedProductForMedia.id);
      setSelectedProductForMedia(updated);
    } catch (err: any) {
      showNotification(err.message, 'error');
    }
  };

  // Delete Feature
  const handleDeleteFeature = async (id: number) => {
    try {
      await fetch(`/api/admin/features?id=${id}`, { method: 'DELETE' });
      showNotification('Feature removed.');
      fetchData();
      if (selectedProductForMedia) {
        setSelectedProductForMedia({
          ...selectedProductForMedia,
          features: selectedProductForMedia.features.filter((f) => f.id !== id)
        });
      }
    } catch (err: any) {
      showNotification(err.message, 'error');
    }
  };

  // Add Screenshot
  const handleAddScreenshot = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedProductForMedia) return;

    try {
      const res = await fetch('/api/admin/screenshots', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          productId: selectedProductForMedia.id,
          ...newScreenshotForm
        })
      });
      if (!res.ok) throw new Error('Failed to attach screenshot');
      showNotification('Screenshot attached to module!');
      setNewScreenshotForm({
        title: '',
        caption: '',
        imagePath: AVAILABLE_IMAGES[0].path,
        category: 'Interface'
      });
      fetchData();
      const updatedProdRes = await fetch('/api/admin/products');
      const updatedData = await updatedProdRes.json();
      const updated = updatedData.products.find((p: any) => p.id === selectedProductForMedia.id);
      setSelectedProductForMedia(updated);
    } catch (err: any) {
      showNotification(err.message, 'error');
    }
  };

  // Delete Screenshot
  const handleDeleteScreenshot = async (id: number) => {
    try {
      await fetch(`/api/admin/screenshots?id=${id}`, { method: 'DELETE' });
      showNotification('Screenshot unlinked.');
      fetchData();
      if (selectedProductForMedia) {
        setSelectedProductForMedia({
          ...selectedProductForMedia,
          screenshots: selectedProductForMedia.screenshots.filter((s) => s.id !== id)
        });
      }
    } catch (err: any) {
      showNotification(err.message, 'error');
    }
  };

  // Save Hero Content
  const handleSaveHero = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const res = await fetch('/api/admin/hero', {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(heroContent)
      });
      if (!res.ok) throw new Error('Failed to update Hero configuration');
      showNotification('Hero banner configuration updated in MySQL!');
      fetchData();
    } catch (err: any) {
      showNotification(err.message, 'error');
    }
  };

  const openEditModal = (p: ProductItem) => {
    setEditingProduct(p);
    setProductForm({
      name: p.name,
      slug: p.slug,
      tagline: p.tagline,
      category: p.category,
      badge: p.badge,
      heroImage: p.heroImage,
      summary: p.summary,
      description: p.description,
      icon: p.icon,
      accentColor: p.accentColor,
      specs: p.specs,
      order: p.order
    });
  };

  const openCreateModal = () => {
    setEditingProduct(null);
    setProductForm({
      name: '',
      slug: '',
      tagline: '',
      category: 'Core Playout',
      badge: 'BROADCAST MODULE',
      heroImage: '/assets/products/playout-master-rundown.png',
      summary: '',
      description: '',
      icon: 'Tv',
      accentColor: '#8B5CF6',
      specs: JSON.stringify({
        "Output Standards": "SMPTE ST 2110-20/30/40, DeckLink 12G-SDI, NDI 6",
        "Video Resolutions": "1080i50, 1080p50/60, 2160p50/60 (4K UHD)",
        "Latency": "< 1 Frame Hardware Direct Render",
        "Redundancy": "1+1 Mirror Hot Standby Failover"
      }, null, 2),
      order: products.length + 1
    });
    setIsCreatingProduct(true);
  };

  const categories = ['ALL', ...Array.from(new Set(products.map((p) => p.category)))];

  const filteredProducts = products.filter((p) => {
    const matchCat = selectedCategory === 'ALL' || p.category === selectedCategory;
    const matchQuery =
      p.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
      p.tagline.toLowerCase().includes(searchQuery.toLowerCase()) ||
      p.category.toLowerCase().includes(searchQuery.toLowerCase());
    return matchCat && matchQuery;
  });

  return (
    <div className="min-h-screen bg-[var(--bg-page)] text-[var(--text-main)] flex">
      {/* Toast Notification */}
      {msg && (
        <div className="fixed bottom-6 right-6 z-50 flex items-center gap-3 px-5 py-3 rounded-xl bg-[var(--bg-card)] border border-[var(--border-strong)] shadow-2xl backdrop-blur-xl animate-in slide-in-from-bottom-5">
          {msg.type === 'success' ? (
            <div className="w-8 h-8 rounded-full bg-emerald-500/10 border border-emerald-500/30 flex items-center justify-center text-emerald-400">
              <CheckCircle2 className="w-4 h-4" />
            </div>
          ) : (
            <div className="w-8 h-8 rounded-full bg-red-500/10 border border-red-500/30 flex items-center justify-center text-red-400">
              <AlertCircle className="w-4 h-4" />
            </div>
          )}
          <span className="text-sm font-medium">{msg.text}</span>
          <button onClick={() => setMsg(null)} className="text-[var(--text-muted)] hover:text-white ml-2">
            <X className="w-4 h-4" />
          </button>
        </div>
      )}

      {/* Lightbox for screenshots */}
      {activeLightboxImg && (
        <div className="fixed inset-0 z-50 bg-black/90 backdrop-blur-md flex items-center justify-center p-6" onClick={() => setActiveLightboxImg(null)}>
          <div className="relative max-w-6xl w-full bg-[var(--bg-card)] border border-[var(--border-strong)] rounded-2xl overflow-hidden shadow-2xl" onClick={(e) => e.stopPropagation()}>
            <div className="flex items-center justify-between px-6 py-4 border-b border-[var(--border-subtle)] bg-[var(--bg-surface)]">
              <div className="flex items-center gap-2">
                <span className="w-2.5 h-2.5 rounded-full bg-emerald-400 animate-pulse" />
                <span className="font-mono text-xs uppercase tracking-wider text-[var(--text-muted)]">{activeLightboxImg}</span>
              </div>
              <div className="flex items-center gap-3">
                <button 
                  onClick={() => copyToClipboard(activeLightboxImg)}
                  className="px-3 py-1.5 rounded-lg bg-[var(--bg-card)] border border-[var(--border-subtle)] hover:border-[var(--accent)] text-xs font-medium flex items-center gap-1.5 transition-colors"
                >
                  <Copy className="w-3.5 h-3.5" />
                  <span>Copy Path</span>
                </button>
                <button onClick={() => setActiveLightboxImg(null)} className="p-1.5 rounded-lg hover:bg-white/10 text-[var(--text-muted)] hover:text-white transition-colors">
                  <X className="w-5 h-5" />
                </button>
              </div>
            </div>
            <div className="p-4 bg-black/60 flex items-center justify-center max-h-[80vh] overflow-hidden">
              <img src={activeLightboxImg} alt="Preview" className="max-h-[75vh] w-auto object-contain rounded-lg shadow-2xl" />
            </div>
          </div>
        </div>
      )}

      {/* Modern Enterprise Sidebar (Shadcn Dashboard Style) */}
      <aside className="w-72 bg-[var(--bg-surface)] border-r border-[var(--border-subtle)] flex flex-col justify-between shrink-0 sticky top-0 h-screen select-none z-30">
        <div>
          {/* Brand Header */}
          <div className="p-5 border-b border-[var(--border-subtle)]">
            <Link href="/" className="flex items-center gap-3 group">
              <div className="w-10 h-10 rounded-xl bg-gradient-to-tr from-[#7C3AED] via-[#8B5CF6] to-[#EC4899] p-0.5 shadow-lg shadow-[#7C3AED]/20">
                <div className="w-full h-full bg-[#0B0E13] rounded-[10px] flex items-center justify-center">
                  <Tv className="w-5 h-5 text-[#C4B5FD] group-hover:scale-110 transition-transform" />
                </div>
              </div>
              <div>
                <div className="flex items-center gap-2">
                  <span className="font-black tracking-tight text-base">KASHTRIX</span>
                  <span className="text-[10px] font-mono px-1.5 py-0.5 rounded bg-[var(--accent)]/15 text-[var(--accent-light)] border border-[var(--accent)]/30 font-semibold">
                    PRO MCR
                  </span>
                </div>
                <div className="text-[11px] text-[var(--text-muted)] font-mono flex items-center gap-1 mt-0.5">
                  <span className="w-2 h-2 rounded-full bg-emerald-500 animate-pulse" />
                  <span>NODE-1 • DIRECTX 11</span>
                </div>
              </div>
            </Link>
          </div>

          {/* Quick MCR Node Indicator Bar */}
          <div className="px-5 py-3 border-b border-[var(--border-subtle)] bg-[var(--bg-card)]/50">
            <div className="flex items-center justify-between text-xs">
              <span className="text-[var(--text-muted)] font-mono text-[11px] uppercase tracking-wider">MCR Hub Master</span>
              <span className="px-2 py-0.5 rounded-full bg-red-500/10 border border-red-500/30 text-[10px] font-mono font-bold text-red-400 flex items-center gap-1 animate-pulse">
                <span className="w-1.5 h-1.5 rounded-full bg-red-500" />
                CH-1 LIVE
              </span>
            </div>
          </div>

          {/* Navigation Groups */}
          <nav className="p-4 space-y-6 overflow-y-auto max-h-[calc(100vh-220px)]">
            <div>
              <div className="px-3 mb-2 text-[10px] font-mono font-bold tracking-widest text-[var(--text-muted)] uppercase">
                Core Broadcast Platform
              </div>
              <div className="space-y-1">
                <button
                  onClick={() => setActiveSection('overview')}
                  className={`w-full flex items-center justify-between px-3.5 py-2.5 rounded-xl text-xs font-semibold transition-all ${
                    activeSection === 'overview'
                      ? 'bg-[var(--accent)] text-white shadow-lg shadow-[var(--accent)]/25'
                      : 'text-[var(--text-secondary)] hover:text-[var(--text-main)] hover:bg-[var(--bg-card-hover)]'
                  }`}
                >
                  <div className="flex items-center gap-3">
                    <LayoutDashboard className="w-4 h-4" />
                    <span>Dashboard Overview</span>
                  </div>
                  <span className={`text-[10px] font-mono px-1.5 py-0.5 rounded ${
                    activeSection === 'overview' ? 'bg-white/20 text-white' : 'bg-[var(--border-subtle)] text-[var(--text-muted)]'
                  }`}>
                    LIVE
                  </span>
                </button>

                <button
                  onClick={() => setActiveSection('products')}
                  className={`w-full flex items-center justify-between px-3.5 py-2.5 rounded-xl text-xs font-semibold transition-all ${
                    activeSection === 'products'
                      ? 'bg-[var(--accent)] text-white shadow-lg shadow-[var(--accent)]/25'
                      : 'text-[var(--text-secondary)] hover:text-[var(--text-main)] hover:bg-[var(--bg-card-hover)]'
                  }`}
                >
                  <div className="flex items-center gap-3">
                    <Tv className="w-4 h-4" />
                    <span>Broadcast Modules</span>
                  </div>
                  <span className={`text-[10px] font-mono px-1.5 py-0.5 rounded font-bold ${
                    activeSection === 'products' ? 'bg-white/20 text-white' : 'bg-[var(--border-subtle)] text-[var(--text-muted)]'
                  }`}>
                    {products.length}
                  </span>
                </button>

                <button
                  onClick={() => setActiveSection('media')}
                  className={`w-full flex items-center justify-between px-3.5 py-2.5 rounded-xl text-xs font-semibold transition-all ${
                    activeSection === 'media'
                      ? 'bg-[var(--accent)] text-white shadow-lg shadow-[var(--accent)]/25'
                      : 'text-[var(--text-secondary)] hover:text-[var(--text-main)] hover:bg-[var(--bg-card-hover)]'
                  }`}
                >
                  <div className="flex items-center gap-3">
                    <FolderKanban className="w-4 h-4" />
                    <span>Media Asset Vault</span>
                  </div>
                  <span className={`text-[10px] font-mono px-1.5 py-0.5 rounded ${
                    activeSection === 'media' ? 'bg-white/20 text-white' : 'bg-[var(--border-subtle)] text-[var(--text-muted)]'
                  }`}>
                    {AVAILABLE_IMAGES.length}
                  </span>
                </button>
              </div>
            </div>

            <div>
              <div className="px-3 mb-2 text-[10px] font-mono font-bold tracking-widest text-[var(--text-muted)] uppercase">
                Content & Inquiries
              </div>
              <div className="space-y-1">
                <button
                  onClick={() => setActiveSection('hero')}
                  className={`w-full flex items-center justify-between px-3.5 py-2.5 rounded-xl text-xs font-semibold transition-all ${
                    activeSection === 'hero'
                      ? 'bg-[var(--accent)] text-white shadow-lg shadow-[var(--accent)]/25'
                      : 'text-[var(--text-secondary)] hover:text-[var(--text-main)] hover:bg-[var(--bg-card-hover)]'
                  }`}
                >
                  <div className="flex items-center gap-3">
                    <Sparkles className="w-4 h-4" />
                    <span>Hero & Telemetry</span>
                  </div>
                </button>

                <button
                  onClick={() => setActiveSection('inquiries')}
                  className={`w-full flex items-center justify-between px-3.5 py-2.5 rounded-xl text-xs font-semibold transition-all ${
                    activeSection === 'inquiries'
                      ? 'bg-[var(--accent)] text-white shadow-lg shadow-[var(--accent)]/25'
                      : 'text-[var(--text-secondary)] hover:text-[var(--text-main)] hover:bg-[var(--bg-card-hover)]'
                  }`}
                >
                  <div className="flex items-center gap-3">
                    <Mail className="w-4 h-4" />
                    <span>Demo Inquiries</span>
                  </div>
                  {inquiries.length > 0 && (
                    <span className="text-[10px] font-mono px-1.5 py-0.5 rounded bg-emerald-500/20 text-emerald-400 font-bold border border-emerald-500/30">
                      {inquiries.length}
                    </span>
                  )}
                </button>
              </div>
            </div>

            <div>
              <div className="px-3 mb-2 text-[10px] font-mono font-bold tracking-widest text-[var(--text-muted)] uppercase">
                Infrastructure & DB
              </div>
              <div className="space-y-1">
                <button
                  onClick={() => setActiveSection('system')}
                  className={`w-full flex items-center justify-between px-3.5 py-2.5 rounded-xl text-xs font-semibold transition-all ${
                    activeSection === 'system'
                      ? 'bg-[var(--accent)] text-white shadow-lg shadow-[var(--accent)]/25'
                      : 'text-[var(--text-secondary)] hover:text-[var(--text-main)] hover:bg-[var(--bg-card-hover)]'
                  }`}
                >
                  <div className="flex items-center gap-3">
                    <Database className="w-4 h-4" />
                    <span>MySQL 9.1 & Prisma</span>
                  </div>
                  <span className="w-2 h-2 rounded-full bg-emerald-400" />
                </button>
              </div>
            </div>
          </nav>
        </div>

        {/* Sidebar Footer / User Profile & Theme */}
        <div className="p-4 border-t border-[var(--border-subtle)] bg-[var(--bg-surface)]">
          <div className="flex items-center justify-between mb-3 px-1">
            <button
              onClick={toggleTheme}
              className="px-2.5 py-1.5 rounded-lg border border-[var(--border-subtle)] hover:border-[var(--border-strong)] text-xs font-medium flex items-center gap-2 text-[var(--text-secondary)] hover:text-[var(--text-main)] transition-colors"
            >
              {theme === 'dark' ? <Sun className="w-3.5 h-3.5 text-amber-400" /> : <Moon className="w-3.5 h-3.5 text-indigo-400" />}
              <span>{theme === 'dark' ? 'Light Mode' : 'Dark Mode'}</span>
            </button>

            <Link
              href="/"
              target="_blank"
              className="p-1.5 rounded-lg border border-[var(--border-subtle)] hover:border-[var(--border-strong)] text-[var(--text-secondary)] hover:text-[var(--text-main)] transition-colors"
              title="Open Public Website"
            >
              <ExternalLink className="w-3.5 h-3.5" />
            </Link>
          </div>

          <div className="p-2.5 rounded-xl bg-[var(--bg-card)] border border-[var(--border-subtle)] flex items-center gap-3">
            <div className="w-8 h-8 rounded-lg bg-[var(--accent)]/20 border border-[var(--accent)]/40 flex items-center justify-center font-bold text-xs text-[var(--accent-light)]">
              MCR
            </div>
            <div className="flex-1 min-w-0">
              <div className="text-xs font-semibold truncate">Master Control Eng.</div>
              <div className="text-[10px] font-mono text-[var(--text-muted)] truncate">root@127.0.0.1:3306</div>
            </div>
            <span className="w-2 h-2 rounded-full bg-emerald-400 animate-pulse" />
          </div>
        </div>
      </aside>

      {/* Main Content Area */}
      <main className="flex-1 flex flex-col min-w-0 overflow-y-auto">
        {/* Top Header Bar */}
        <header className="h-16 border-b border-[var(--border-subtle)] bg-[var(--bg-surface)]/80 backdrop-blur-md sticky top-0 z-20 flex items-center justify-between px-8">
          <div className="flex items-center gap-3 text-xs">
            <span className="text-[var(--text-muted)] font-mono">Kashtrix MCR</span>
            <span className="text-[var(--text-muted)]">/</span>
            <span className="font-semibold capitalize text-[var(--text-main)]">
              {activeSection === 'overview' && 'Dashboard Overview'}
              {activeSection === 'products' && 'Broadcast Modules'}
              {activeSection === 'media' && 'Media Asset Vault'}
              {activeSection === 'hero' && 'Hero & Telemetry'}
              {activeSection === 'inquiries' && 'Demo Inquiries CRM'}
              {activeSection === 'system' && 'Infrastructure & Database'}
            </span>
          </div>

          <div className="flex items-center gap-3">
            {/* Live Tally Bar */}
            <div className="hidden lg:flex items-center gap-2 px-3 py-1 rounded-full bg-[var(--bg-card)] border border-[var(--border-subtle)] text-[11px] font-mono">
              <span className="flex items-center gap-1.5 text-red-400 font-semibold">
                <span className="w-2 h-2 rounded-full bg-red-500 animate-pulse" />
                ST 2110-20 ON AIR
              </span>
              <span className="text-[var(--border-strong)]">|</span>
              <span className="text-emerald-400">50.00 FPS DIRECTX 11</span>
            </div>

            <button
              onClick={fetchData}
              disabled={loading}
              className="p-2 rounded-lg border border-[var(--border-subtle)] hover:bg-[var(--bg-card-hover)] text-[var(--text-secondary)] hover:text-[var(--text-main)] transition-colors"
              title="Refresh Data"
            >
              <RefreshCw className={`w-4 h-4 ${loading ? 'animate-spin' : ''}`} />
            </button>

            {activeSection === 'products' && (
              <button
                onClick={openCreateModal}
                className="px-4 py-2 rounded-xl bg-gradient-to-r from-[#7C3AED] to-[#A855F7] text-white text-xs font-semibold flex items-center gap-2 shadow-lg shadow-[#7C3AED]/20 hover:opacity-95 transition-all"
              >
                <Plus className="w-4 h-4" />
                <span>Add Module</span>
              </button>
            )}
          </div>
        </header>

        {/* Content Body */}
        <div className="p-8 max-w-7xl w-full mx-auto space-y-8">
          
          {/* TAB 1: OVERVIEW */}
          {activeSection === 'overview' && (
            <div className="space-y-8">
              {/* Header Title */}
              <div className="flex items-center justify-between">
                <div>
                  <h1 className="text-2xl font-black tracking-tight">System Telemetry & MCR Dashboard</h1>
                  <p className="text-sm text-[var(--text-muted)] mt-1">
                    Real-time status of all Kashtrix television automation engines, media assets, and incoming station inquiries.
                  </p>
                </div>
                <div className="flex items-center gap-2">
                  <button
                    onClick={openCreateModal}
                    className="px-4 py-2 rounded-xl bg-[var(--accent)] hover:bg-[var(--accent-dark)] text-white text-xs font-semibold flex items-center gap-2 shadow-md transition-all"
                  >
                    <Plus className="w-4 h-4" />
                    <span>Create Module</span>
                  </button>
                </div>
              </div>

              {/* 4 Shadcn Metric Cards */}
              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5">
                <div className="p-5 rounded-2xl bg-[var(--bg-card)] border border-[var(--border-subtle)] shadow-sm hover:border-[var(--border-strong)] transition-all">
                  <div className="flex items-center justify-between text-xs text-[var(--text-muted)] font-mono uppercase tracking-wider">
                    <span>Active Modules</span>
                    <div className="w-8 h-8 rounded-lg bg-[var(--accent)]/15 border border-[var(--accent)]/30 flex items-center justify-center text-[var(--accent-light)]">
                      <Tv className="w-4 h-4" />
                    </div>
                  </div>
                  <div className="mt-3 flex items-baseline gap-2">
                    <span className="text-3xl font-black tracking-tight">{products.length}</span>
                    <span className="text-xs font-semibold text-emerald-400 flex items-center">
                      <TrendingUp className="w-3 h-3 mr-0.5" /> 100% Online
                    </span>
                  </div>
                  <div className="mt-2 text-xs text-[var(--text-muted)]">
                    All core playout and studio switcher pipelines operational
                  </div>
                </div>

                <div className="p-5 rounded-2xl bg-[var(--bg-card)] border border-[var(--border-subtle)] shadow-sm hover:border-[var(--border-strong)] transition-all">
                  <div className="flex items-center justify-between text-xs text-[var(--text-muted)] font-mono uppercase tracking-wider">
                    <span>Media Asset Vault</span>
                    <div className="w-8 h-8 rounded-lg bg-pink-500/15 border border-pink-500/30 flex items-center justify-center text-pink-400">
                      <ImageIcon className="w-4 h-4" />
                    </div>
                  </div>
                  <div className="mt-3 flex items-baseline gap-2">
                    <span className="text-3xl font-black tracking-tight">{AVAILABLE_IMAGES.length}</span>
                    <span className="text-xs font-semibold text-indigo-400">4K UHD</span>
                  </div>
                  <div className="mt-2 text-xs text-[var(--text-muted)]">
                    Authentic high-resolution application screenshots mapped
                  </div>
                </div>

                <div className="p-5 rounded-2xl bg-[var(--bg-card)] border border-[var(--border-subtle)] shadow-sm hover:border-[var(--border-strong)] transition-all">
                  <div className="flex items-center justify-between text-xs text-[var(--text-muted)] font-mono uppercase tracking-wider">
                    <span>Demo Inquiries</span>
                    <div className="w-8 h-8 rounded-lg bg-amber-500/15 border border-amber-500/30 flex items-center justify-center text-amber-400">
                      <Inbox className="w-4 h-4" />
                    </div>
                  </div>
                  <div className="mt-3 flex items-baseline gap-2">
                    <span className="text-3xl font-black tracking-tight">{inquiries.length}</span>
                    <span className="text-xs font-semibold text-amber-400">Pending Review</span>
                  </div>
                  <div className="mt-2 text-xs text-[var(--text-muted)]">
                    Direct lead submissions from prospective broadcast networks
                  </div>
                </div>

                <div className="p-5 rounded-2xl bg-[var(--bg-card)] border border-[var(--border-subtle)] shadow-sm hover:border-[var(--border-strong)] transition-all">
                  <div className="flex items-center justify-between text-xs text-[var(--text-muted)] font-mono uppercase tracking-wider">
                    <span>Transmission SLA</span>
                    <div className="w-8 h-8 rounded-lg bg-emerald-500/15 border border-emerald-500/30 flex items-center justify-center text-emerald-400">
                      <ShieldCheck className="w-4 h-4" />
                    </div>
                  </div>
                  <div className="mt-3 flex items-baseline gap-2">
                    <span className="text-3xl font-black tracking-tight text-emerald-400">99.999%</span>
                  </div>
                  <div className="mt-2 text-xs text-[var(--text-muted)]">
                    Dual 1+1 mirror redundancy cluster hot-synced
                  </div>
                </div>
              </div>

              {/* Real-Time Video Signal Matrix (High-Tech Broadcast Console) */}
              <div className="rounded-2xl bg-[var(--bg-card)] border border-[var(--border-subtle)] overflow-hidden shadow-sm">
                <div className="p-5 border-b border-[var(--border-subtle)] flex items-center justify-between bg-[var(--bg-surface)]/50">
                  <div className="flex items-center gap-3">
                    <Activity className="w-5 h-5 text-[var(--accent)]" />
                    <div>
                      <h3 className="font-bold text-sm">Real-Time Broadcast Engine Signal Matrix</h3>
                      <p className="text-xs text-[var(--text-muted)]">Hardware baseband SDI, ST 2110-20 uncompressed video & audio status</p>
                    </div>
                  </div>
                  <div className="flex items-center gap-2">
                    <span className="px-2.5 py-1 rounded-md bg-emerald-500/10 border border-emerald-500/30 text-[11px] font-mono font-semibold text-emerald-400 flex items-center gap-1.5">
                      <span className="w-2 h-2 rounded-full bg-emerald-400 animate-pulse" />
                      ALL 4 HUBS LOCKED
                    </span>
                  </div>
                </div>

                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 divide-y md:divide-y-0 md:divide-x divide-[var(--border-subtle)]">
                  {/* Playout Signal */}
                  <div className="p-5 space-y-3">
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-2">
                        <Tv className="w-4 h-4 text-[#8B5CF6]" />
                        <span className="font-bold text-xs">Master Playout</span>
                      </div>
                      <span className="px-2 py-0.5 rounded text-[10px] font-mono bg-red-500/10 text-red-400 border border-red-500/30 font-bold">
                        ON AIR PGM
                      </span>
                    </div>
                    <div className="rounded-lg overflow-hidden border border-[var(--border-subtle)] aspect-video bg-black/40 relative group cursor-pointer" onClick={() => setActiveLightboxImg('/assets/products/playout-master-rundown.png')}>
                      <img src="/assets/products/playout-master-rundown.png" alt="Playout" className="w-full h-full object-cover group-hover:scale-105 transition-transform" />
                      <div className="absolute inset-0 bg-black/40 opacity-0 group-hover:opacity-100 transition-opacity flex items-center justify-center">
                        <Maximize2 className="w-5 h-5 text-white" />
                      </div>
                    </div>
                    <div className="space-y-1 text-[11px] font-mono text-[var(--text-muted)]">
                      <div className="flex justify-between"><span>Format:</span> <span className="text-[var(--text-main)]">1080p50 ST 2110-20</span></div>
                      <div className="flex justify-between"><span>FPS:</span> <span className="text-emerald-400 font-bold">50.00 FPS Locked</span></div>
                      <div className="flex justify-between"><span>Loudness:</span> <span className="text-[var(--text-main)]">-23.0 LUFS EBU R128</span></div>
                    </div>
                  </div>

                  {/* Studio Switcher Signal */}
                  <div className="p-5 space-y-3">
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-2">
                        <Video className="w-4 h-4 text-[#EC4899]" />
                        <span className="font-bold text-xs">Studio Switcher</span>
                      </div>
                      <span className="px-2 py-0.5 rounded text-[10px] font-mono bg-emerald-500/10 text-emerald-400 border border-emerald-500/30 font-bold">
                        PVW ARMED
                      </span>
                    </div>
                    <div className="rounded-lg overflow-hidden border border-[var(--border-subtle)] aspect-video bg-black/40 relative group cursor-pointer" onClick={() => setActiveLightboxImg('/assets/products/studio-switcher-full.png')}>
                      <img src="/assets/products/studio-switcher-full.png" alt="Studio" className="w-full h-full object-cover group-hover:scale-105 transition-transform" />
                      <div className="absolute inset-0 bg-black/40 opacity-0 group-hover:opacity-100 transition-opacity flex items-center justify-center">
                        <Maximize2 className="w-5 h-5 text-white" />
                      </div>
                    </div>
                    <div className="space-y-1 text-[11px] font-mono text-[var(--text-muted)]">
                      <div className="flex justify-between"><span>Inputs:</span> <span className="text-[var(--text-main)]">12 Channels Active</span></div>
                      <div className="flex justify-between"><span>Multiview:</span> <span className="text-emerald-400 font-bold">12-CH Layout</span></div>
                      <div className="flex justify-between"><span>DVE Engine:</span> <span className="text-[var(--text-main)]">DirectX 11 Hardware</span></div>
                    </div>
                  </div>

                  {/* 3D CG Compositor */}
                  <div className="p-5 space-y-3">
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-2">
                        <Layers className="w-4 h-4 text-[#38BDF8]" />
                        <span className="font-bold text-xs">3D Motion CG</span>
                      </div>
                      <span className="px-2 py-0.5 rounded text-[10px] font-mono bg-cyan-500/10 text-cyan-400 border border-cyan-500/30 font-bold">
                        KEY/FILL OK
                      </span>
                    </div>
                    <div className="rounded-lg overflow-hidden border border-[var(--border-subtle)] aspect-video bg-black/40 relative group cursor-pointer" onClick={() => setActiveLightboxImg('/assets/products/cg-editor-main.png')}>
                      <img src="/assets/products/cg-editor-main.png" alt="CG" className="w-full h-full object-cover group-hover:scale-105 transition-transform" />
                      <div className="absolute inset-0 bg-black/40 opacity-0 group-hover:opacity-100 transition-opacity flex items-center justify-center">
                        <Maximize2 className="w-5 h-5 text-white" />
                      </div>
                    </div>
                    <div className="space-y-1 text-[11px] font-mono text-[var(--text-muted)]">
                      <div className="flex justify-between"><span>Layers:</span> <span className="text-[var(--text-main)]">4 Real-Time Tracks</span></div>
                      <div className="flex justify-between"><span>Data Feed:</span> <span className="text-emerald-400 font-bold">MOS 2.8.4 JSON</span></div>
                      <div className="flex justify-between"><span>Alpha:</span> <span className="text-[var(--text-main)]">Linear Alpha 32-bit</span></div>
                    </div>
                  </div>

                  {/* MAM & Ingest */}
                  <div className="p-5 space-y-3">
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-2">
                        <Database className="w-4 h-4 text-[#F59E0B]" />
                        <span className="font-bold text-xs">MAM & Storage</span>
                      </div>
                      <span className="px-2 py-0.5 rounded text-[10px] font-mono bg-indigo-500/10 text-indigo-400 border border-indigo-500/30 font-bold">
                        STORAGE OK
                      </span>
                    </div>
                    <div className="rounded-lg overflow-hidden border border-[var(--border-subtle)] aspect-video bg-black/40 relative group cursor-pointer" onClick={() => setActiveLightboxImg('/assets/products/mam-catalog.png')}>
                      <img src="/assets/products/mam-catalog.png" alt="MAM" className="w-full h-full object-cover group-hover:scale-105 transition-transform" />
                      <div className="absolute inset-0 bg-black/40 opacity-0 group-hover:opacity-100 transition-opacity flex items-center justify-center">
                        <Maximize2 className="w-5 h-5 text-white" />
                      </div>
                    </div>
                    <div className="space-y-1 text-[11px] font-mono text-[var(--text-muted)]">
                      <div className="flex justify-between"><span>Fast NVMe:</span> <span className="text-[var(--text-main)]">32 TB (82% Free)</span></div>
                      <div className="flex justify-between"><span>Transcoder:</span> <span className="text-emerald-400 font-bold">ProRes 422 HQ</span></div>
                      <div className="flex justify-between"><span>Ingest Status:</span> <span className="text-[var(--text-main)]">Auto-QC Pass</span></div>
                    </div>
                  </div>
                </div>
              </div>

              {/* Fast Access Product Cards Grid */}
              <div className="space-y-4">
                <div className="flex items-center justify-between">
                  <h3 className="font-bold text-base">Broadcast Modules Catalog (Fast Access)</h3>
                  <button 
                    onClick={() => setActiveSection('products')}
                    className="text-xs font-semibold text-[var(--accent)] hover:underline flex items-center gap-1"
                  >
                    <span>View Full Data Table</span>
                    <ChevronRight className="w-3.5 h-3.5" />
                  </button>
                </div>

                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                  {products.slice(0, 6).map((p) => (
                    <div
                      key={p.id}
                      className="p-4 rounded-xl bg-[var(--bg-card)] border border-[var(--border-subtle)] hover:border-[var(--border-strong)] transition-all flex items-center justify-between group"
                    >
                      <div className="flex items-center gap-3 min-w-0">
                        <div className="w-10 h-10 rounded-lg bg-[var(--bg-surface)] border border-[var(--border-subtle)] flex items-center justify-center text-[var(--accent)] shrink-0">
                          <Icon name={p.icon} className="w-5 h-5" />
                        </div>
                        <div className="min-w-0">
                          <div className="font-semibold text-xs truncate group-hover:text-[var(--accent)] transition-colors">{p.name}</div>
                          <div className="text-[10px] text-[var(--text-muted)] font-mono truncate">{p.category}</div>
                        </div>
                      </div>

                      <div className="flex items-center gap-1 shrink-0 ml-2">
                        <button
                          onClick={() => {
                            setSelectedProductForMedia(p);
                            setActiveSection('products');
                          }}
                          className="p-1.5 rounded-md hover:bg-[var(--bg-surface)] text-[var(--text-muted)] hover:text-white transition-colors"
                          title="Manage Media & Features"
                        >
                          <ImageIcon className="w-3.5 h-3.5" />
                        </button>
                        <button
                          onClick={() => {
                            openEditModal(p);
                          }}
                          className="p-1.5 rounded-md hover:bg-[var(--bg-surface)] text-[var(--text-muted)] hover:text-white transition-colors"
                          title="Edit Module Specs"
                        >
                          <Edit className="w-3.5 h-3.5" />
                        </button>
                        <Link
                          href={`/products/${p.slug}`}
                          target="_blank"
                          className="p-1.5 rounded-md hover:bg-[var(--bg-surface)] text-[var(--text-muted)] hover:text-white transition-colors"
                          title="View Live Page"
                        >
                          <ArrowUpRight className="w-3.5 h-3.5" />
                        </Link>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          )}

          {/* TAB 2: BROADCAST MODULES (PRODUCTS CRUD) */}
          {activeSection === 'products' && (
            <div className="space-y-6">
              {/* Top controls: Search, Categories, and View Mode */}
              <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
                <div>
                  <h1 className="text-2xl font-black tracking-tight">Broadcast Suite Modules</h1>
                  <p className="text-sm text-[var(--text-muted)] mt-1">
                    Manage 14+ broadcast playout, switcher, graphics, and MAM applications in MySQL.
                  </p>
                </div>

                <div className="flex items-center gap-3">
                  <div className="relative">
                    <Search className="w-4 h-4 absolute left-3 top-1/2 -translate-y-1/2 text-[var(--text-muted)]" />
                    <input
                      type="text"
                      placeholder="Search module name or category..."
                      value={searchQuery}
                      onChange={(e) => setSearchQuery(e.target.value)}
                      className="pl-9 pr-4 py-2 rounded-xl bg-[var(--bg-card)] border border-[var(--border-subtle)] text-xs text-[var(--text-main)] placeholder-[var(--text-muted)] focus:outline-none focus:border-[var(--accent)] w-64 transition-all"
                    />
                  </div>

                  <div className="flex items-center bg-[var(--bg-card)] border border-[var(--border-subtle)] rounded-xl p-1">
                    <button
                      onClick={() => setProductViewMode('table')}
                      className={`px-3 py-1.5 rounded-lg text-xs font-semibold transition-all ${
                        productViewMode === 'table' ? 'bg-[var(--accent)] text-white' : 'text-[var(--text-muted)] hover:text-[var(--text-main)]'
                      }`}
                    >
                      Table View
                    </button>
                    <button
                      onClick={() => setProductViewMode('grid')}
                      className={`px-3 py-1.5 rounded-lg text-xs font-semibold transition-all ${
                        productViewMode === 'grid' ? 'bg-[var(--accent)] text-white' : 'text-[var(--text-muted)] hover:text-[var(--text-main)]'
                      }`}
                    >
                      Card Grid
                    </button>
                  </div>
                </div>
              </div>

              {/* Category Filter Pills */}
              <div className="flex items-center gap-2 overflow-x-auto pb-2">
                {categories.map((cat) => (
                  <button
                    key={cat}
                    onClick={() => setSelectedCategory(cat)}
                    className={`px-3.5 py-1.5 rounded-full text-xs font-semibold whitespace-nowrap transition-all ${
                      selectedCategory === cat
                        ? 'bg-[var(--accent)] text-white shadow-sm'
                        : 'bg-[var(--bg-card)] border border-[var(--border-subtle)] text-[var(--text-secondary)] hover:border-[var(--border-strong)] hover:text-[var(--text-main)]'
                    }`}
                  >
                    {cat}
                  </button>
                ))}
              </div>

              {/* TABLE VIEW */}
              {productViewMode === 'table' ? (
                <div className="rounded-2xl bg-[var(--bg-card)] border border-[var(--border-subtle)] overflow-hidden shadow-sm">
                  <div className="overflow-x-auto">
                    <table className="w-full text-left border-collapse">
                      <thead>
                        <tr className="border-b border-[var(--border-subtle)] bg-[var(--bg-surface)] text-[11px] font-mono uppercase tracking-wider text-[var(--text-muted)]">
                          <th className="py-3 px-4">Preview</th>
                          <th className="py-3 px-4">Module Name</th>
                          <th className="py-3 px-4">Category</th>
                          <th className="py-3 px-4">Slug & Route</th>
                          <th className="py-3 px-4">Features</th>
                          <th className="py-3 px-4">Media</th>
                          <th className="py-3 px-4 text-right">Actions</th>
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-[var(--border-subtle)] text-xs">
                        {filteredProducts.map((p) => (
                          <tr key={p.id} className="hover:bg-[var(--bg-card-hover)] transition-colors">
                            <td className="py-3 px-4">
                              <div
                                className="w-16 h-10 rounded-lg overflow-hidden border border-[var(--border-subtle)] bg-black/40 cursor-pointer relative group shrink-0"
                                onClick={() => setActiveLightboxImg(p.heroImage)}
                              >
                                <img src={p.heroImage} alt={p.name} className="w-full h-full object-cover group-hover:scale-105 transition-transform" />
                              </div>
                            </td>
                            <td className="py-3 px-4">
                              <div className="font-bold text-[var(--text-main)] flex items-center gap-2">
                                <Icon name={p.icon} className="w-4 h-4 text-[var(--accent)]" />
                                <span>{p.name}</span>
                              </div>
                              <div className="text-[11px] text-[var(--text-muted)] truncate max-w-xs">{p.tagline}</div>
                            </td>
                            <td className="py-3 px-4">
                              <span className="px-2.5 py-1 rounded-full text-[10px] font-mono font-semibold bg-[var(--accent)]/10 text-[var(--accent-light)] border border-[var(--accent)]/20">
                                {p.category}
                              </span>
                            </td>
                            <td className="py-3 px-4 font-mono text-[11px] text-[var(--text-muted)]">
                              /products/{p.slug}
                            </td>
                            <td className="py-3 px-4">
                              <span className="px-2 py-0.5 rounded text-[10px] font-mono bg-emerald-500/10 text-emerald-400 border border-emerald-500/20 font-semibold">
                                {p.features?.length || 0} items
                              </span>
                            </td>
                            <td className="py-3 px-4">
                              <span className="px-2 py-0.5 rounded text-[10px] font-mono bg-indigo-500/10 text-indigo-400 border border-indigo-500/20 font-semibold">
                                {p.screenshots?.length || 0} screens
                              </span>
                            </td>
                            <td className="py-3 px-4 text-right">
                              <div className="flex items-center justify-end gap-1.5">
                                <button
                                  onClick={() => setSelectedProductForMedia(p)}
                                  className="px-2.5 py-1.5 rounded-lg border border-[var(--border-subtle)] hover:border-[var(--accent)] text-[11px] font-medium flex items-center gap-1.5 text-[var(--text-secondary)] hover:text-[var(--text-main)] transition-colors"
                                  title="Manage Media & Features"
                                >
                                  <FolderKanban className="w-3.5 h-3.5" />
                                  <span>Assets</span>
                                </button>
                                <button
                                  onClick={() => openEditModal(p)}
                                  className="p-1.5 rounded-lg border border-[var(--border-subtle)] hover:border-[var(--accent)] text-[var(--text-secondary)] hover:text-[var(--text-main)] transition-colors"
                                  title="Edit Module"
                                >
                                  <Edit className="w-3.5 h-3.5" />
                                </button>
                                <Link
                                  href={`/products/${p.slug}`}
                                  target="_blank"
                                  className="p-1.5 rounded-lg border border-[var(--border-subtle)] hover:border-[var(--accent)] text-[var(--text-secondary)] hover:text-[var(--text-main)] transition-colors"
                                  title="Preview Page"
                                >
                                  <ExternalLink className="w-3.5 h-3.5" />
                                </Link>
                                <button
                                  onClick={() => handleDeleteProduct(p.id, p.name)}
                                  className="p-1.5 rounded-lg border border-[var(--border-subtle)] hover:border-red-500 hover:text-red-400 text-[var(--text-muted)] transition-colors"
                                  title="Delete Module"
                                >
                                  <Trash2 className="w-3.5 h-3.5" />
                                </button>
                              </div>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </div>
              ) : (
                /* CARD GRID VIEW */
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                  {filteredProducts.map((p) => (
                    <div
                      key={p.id}
                      className="rounded-2xl bg-[var(--bg-card)] border border-[var(--border-subtle)] overflow-hidden hover:border-[var(--border-strong)] transition-all shadow-sm flex flex-col justify-between"
                    >
                      <div>
                        <div
                          className="aspect-video bg-black/40 relative cursor-pointer group overflow-hidden"
                          onClick={() => setActiveLightboxImg(p.heroImage)}
                        >
                          <img src={p.heroImage} alt={p.name} className="w-full h-full object-cover group-hover:scale-105 transition-transform" />
                          <div className="absolute top-3 left-3">
                            <span className="px-2.5 py-1 rounded-full text-[10px] font-mono font-semibold bg-black/70 backdrop-blur-md text-white border border-white/20">
                              {p.category}
                            </span>
                          </div>
                        </div>

                        <div className="p-5">
                          <div className="flex items-center gap-2 mb-1.5">
                            <Icon name={p.icon} className="w-4 h-4 text-[var(--accent)]" />
                            <h3 className="font-bold text-sm text-[var(--text-main)]">{p.name}</h3>
                          </div>
                          <p className="text-xs text-[var(--text-muted)] line-clamp-2">{p.tagline}</p>

                          <div className="flex items-center gap-2 mt-4 text-[11px] font-mono text-[var(--text-muted)]">
                            <span className="px-2 py-0.5 rounded bg-[var(--bg-surface)] border border-[var(--border-subtle)]">
                              {p.features?.length || 0} features
                            </span>
                            <span className="px-2 py-0.5 rounded bg-[var(--bg-surface)] border border-[var(--border-subtle)]">
                              {p.screenshots?.length || 0} screens
                            </span>
                          </div>
                        </div>
                      </div>

                      <div className="p-4 border-t border-[var(--border-subtle)] bg-[var(--bg-surface)]/50 flex items-center justify-between">
                        <button
                          onClick={() => setSelectedProductForMedia(p)}
                          className="text-xs font-semibold text-[var(--accent)] hover:underline flex items-center gap-1"
                        >
                          <FolderKanban className="w-3.5 h-3.5" />
                          <span>Manage Media</span>
                        </button>
                        <div className="flex items-center gap-1">
                          <button
                            onClick={() => openEditModal(p)}
                            className="p-1.5 rounded-lg hover:bg-[var(--bg-card)] text-[var(--text-secondary)] hover:text-[var(--text-main)] transition-colors"
                          >
                            <Edit className="w-3.5 h-3.5" />
                          </button>
                          <Link
                            href={`/products/${p.slug}`}
                            target="_blank"
                            className="p-1.5 rounded-lg hover:bg-[var(--bg-card)] text-[var(--text-secondary)] hover:text-[var(--text-main)] transition-colors"
                          >
                            <ArrowUpRight className="w-3.5 h-3.5" />
                          </Link>
                          <button
                            onClick={() => handleDeleteProduct(p.id, p.name)}
                            className="p-1.5 rounded-lg hover:bg-[var(--bg-card)] text-[var(--text-muted)] hover:text-red-400 transition-colors"
                          >
                            <Trash2 className="w-3.5 h-3.5" />
                          </button>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          )}

          {/* TAB 3: MEDIA ASSET VAULT */}
          {activeSection === 'media' && (
            <div className="space-y-6">
              <div className="flex items-center justify-between">
                <div>
                  <h1 className="text-2xl font-black tracking-tight">Broadcast Media Asset Vault</h1>
                  <p className="text-sm text-[var(--text-muted)] mt-1">
                    Authentic 4K UI screenshots captured directly from the Kashtrix Playout, Switcher, CG, and MAM consoles.
                  </p>
                </div>
                <div className="text-xs font-mono px-3 py-1.5 rounded-xl bg-[var(--bg-card)] border border-[var(--border-subtle)] text-[var(--text-muted)]">
                  Total Assets: <span className="text-[var(--text-main)] font-bold">{AVAILABLE_IMAGES.length}</span>
                </div>
              </div>

              {/* Grid of Screenshots */}
              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
                {AVAILABLE_IMAGES.map((img, i) => (
                  <div
                    key={i}
                    className="rounded-2xl bg-[var(--bg-card)] border border-[var(--border-subtle)] overflow-hidden hover:border-[var(--border-strong)] transition-all shadow-sm group"
                  >
                    <div
                      className="aspect-video bg-black/40 relative cursor-pointer overflow-hidden"
                      onClick={() => setActiveLightboxImg(img.path)}
                    >
                      <img src={img.path} alt={img.label} className="w-full h-full object-cover group-hover:scale-105 transition-transform" />
                      <div className="absolute top-3 left-3">
                        <span className="px-2.5 py-1 rounded-full text-[10px] font-mono font-semibold bg-black/70 backdrop-blur-md text-white border border-white/20">
                          {img.category}
                        </span>
                      </div>
                      <div className="absolute inset-0 bg-black/40 opacity-0 group-hover:opacity-100 transition-opacity flex items-center justify-center">
                        <span className="px-3 py-1.5 rounded-lg bg-black/70 backdrop-blur-md text-white text-xs font-medium flex items-center gap-1.5">
                          <Maximize2 className="w-3.5 h-3.5" />
                          View 4K Image
                        </span>
                      </div>
                    </div>

                    <div className="p-4">
                      <div className="font-bold text-xs text-[var(--text-main)] truncate">{img.label}</div>
                      <div className="text-[11px] font-mono text-[var(--text-muted)] truncate mt-0.5">{img.path}</div>

                      <div className="flex items-center justify-between mt-3 pt-3 border-t border-[var(--border-subtle)]">
                        <button
                          onClick={() => copyToClipboard(img.path)}
                          className="px-2.5 py-1 rounded-lg border border-[var(--border-subtle)] hover:border-[var(--accent)] text-[11px] font-medium flex items-center gap-1.5 text-[var(--text-secondary)] hover:text-[var(--text-main)] transition-colors"
                        >
                          {copiedPath === img.path ? <Check className="w-3 h-3 text-emerald-400" /> : <Copy className="w-3 h-3" />}
                          <span>{copiedPath === img.path ? 'Copied!' : 'Copy Path'}</span>
                        </button>

                        <button
                          onClick={() => setActiveLightboxImg(img.path)}
                          className="text-xs font-semibold text-[var(--accent)] hover:underline flex items-center gap-1"
                        >
                          <Eye className="w-3.5 h-3.5" />
                          <span>Inspect</span>
                        </button>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* TAB 4: HERO & TELEMETRY LIVE EDITOR */}
          {activeSection === 'hero' && heroContent && (
            <div className="space-y-6">
              <div>
                <h1 className="text-2xl font-black tracking-tight">Hero Banner & Live Telemetry Editor</h1>
                <p className="text-sm text-[var(--text-muted)] mt-1">
                  Configure the primary marketing copy, top announcement banner, and performance stats stored in MySQL.
                </p>
              </div>

              <form onSubmit={handleSaveHero} className="space-y-6 max-w-4xl">
                <div className="rounded-2xl bg-[var(--bg-card)] border border-[var(--border-subtle)] p-6 space-y-5">
                  <h3 className="font-bold text-sm text-[var(--text-main)] border-b border-[var(--border-subtle)] pb-3">
                    Hero Announcement & Headlines
                  </h3>

                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div>
                      <label className="block text-xs font-semibold text-[var(--text-secondary)] mb-1.5">Top Announcement Badge</label>
                      <input
                        type="text"
                        value={heroContent.badge || ''}
                        onChange={(e) => setHeroContent({ ...heroContent, badge: e.target.value })}
                        className="w-full px-3.5 py-2 rounded-xl bg-[var(--bg-surface)] border border-[var(--border-subtle)] text-xs text-[var(--text-main)] focus:outline-none focus:border-[var(--accent)]"
                      />
                    </div>
                    <div>
                      <label className="block text-xs font-semibold text-[var(--text-secondary)] mb-1.5">Primary Title (Line 1)</label>
                      <input
                        type="text"
                        value={heroContent.title || ''}
                        onChange={(e) => setHeroContent({ ...heroContent, title: e.target.value })}
                        className="w-full px-3.5 py-2 rounded-xl bg-[var(--bg-surface)] border border-[var(--border-subtle)] text-xs text-[var(--text-main)] focus:outline-none focus:border-[var(--accent)]"
                      />
                    </div>
                  </div>

                  <div>
                    <label className="block text-xs font-semibold text-[var(--text-secondary)] mb-1.5">Gradient Highlighted Title</label>
                    <input
                      type="text"
                      value={heroContent.gradientTitle || ''}
                      onChange={(e) => setHeroContent({ ...heroContent, gradientTitle: e.target.value })}
                      className="w-full px-3.5 py-2 rounded-xl bg-[var(--bg-surface)] border border-[var(--border-subtle)] text-xs text-[var(--text-main)] focus:outline-none focus:border-[var(--accent)]"
                    />
                  </div>

                  <div>
                    <label className="block text-xs font-semibold text-[var(--text-secondary)] mb-1.5">Subtitle / Value Proposition</label>
                    <textarea
                      rows={3}
                      value={heroContent.subtitle || ''}
                      onChange={(e) => setHeroContent({ ...heroContent, subtitle: e.target.value })}
                      className="w-full px-3.5 py-2 rounded-xl bg-[var(--bg-surface)] border border-[var(--border-subtle)] text-xs text-[var(--text-main)] focus:outline-none focus:border-[var(--accent)]"
                    />
                  </div>

                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div>
                      <label className="block text-xs font-semibold text-[var(--text-secondary)] mb-1.5">Primary CTA Button</label>
                      <input
                        type="text"
                        value={heroContent.primaryCta || ''}
                        onChange={(e) => setHeroContent({ ...heroContent, primaryCta: e.target.value })}
                        className="w-full px-3.5 py-2 rounded-xl bg-[var(--bg-surface)] border border-[var(--border-subtle)] text-xs text-[var(--text-main)] focus:outline-none focus:border-[var(--accent)]"
                      />
                    </div>
                    <div>
                      <label className="block text-xs font-semibold text-[var(--text-secondary)] mb-1.5">Primary CTA URL</label>
                      <input
                        type="text"
                        value={heroContent.primaryCtaUrl || ''}
                        onChange={(e) => setHeroContent({ ...heroContent, primaryCtaUrl: e.target.value })}
                        className="w-full px-3.5 py-2 rounded-xl bg-[var(--bg-surface)] border border-[var(--border-subtle)] text-xs text-[var(--text-main)] focus:outline-none focus:border-[var(--accent)]"
                      />
                    </div>
                  </div>

                  <div>
                    <label className="block text-xs font-semibold text-[var(--text-secondary)] mb-1.5">Live Telemetry KPI Stats (JSON)</label>
                    <textarea
                      rows={4}
                      value={heroContent.stats || ''}
                      onChange={(e) => setHeroContent({ ...heroContent, stats: e.target.value })}
                      className="w-full px-3.5 py-2 rounded-xl bg-[var(--bg-surface)] border border-[var(--border-subtle)] text-xs font-mono text-[var(--text-main)] focus:outline-none focus:border-[var(--accent)]"
                    />
                  </div>

                  <div className="pt-2 flex justify-end">
                    <button
                      type="submit"
                      className="px-5 py-2.5 rounded-xl bg-gradient-to-r from-[#7C3AED] to-[#A855F7] text-white text-xs font-semibold shadow-md flex items-center gap-2 hover:opacity-95 transition-all"
                    >
                      <Save className="w-4 h-4" />
                      <span>Save Hero Configuration</span>
                    </button>
                  </div>
                </div>
              </form>
            </div>
          )}

          {/* TAB 5: DEMO INQUIRIES CRM */}
          {activeSection === 'inquiries' && (
            <div className="space-y-6">
              <div className="flex items-center justify-between">
                <div>
                  <h1 className="text-2xl font-black tracking-tight">Station Demo Requests & Leads</h1>
                  <p className="text-sm text-[var(--text-muted)] mt-1">
                    Incoming television channel inquiries and proof-of-concept requests submitted via `/contact`.
                  </p>
                </div>
                <div className="text-xs font-mono px-3 py-1.5 rounded-xl bg-[var(--bg-card)] border border-[var(--border-subtle)] text-[var(--text-muted)]">
                  Total Requests: <span className="text-[var(--text-main)] font-bold">{inquiries.length}</span>
                </div>
              </div>

              <div className="rounded-2xl bg-[var(--bg-card)] border border-[var(--border-subtle)] overflow-hidden shadow-sm">
                <div className="overflow-x-auto">
                  <table className="w-full text-left border-collapse">
                    <thead>
                      <tr className="border-b border-[var(--border-subtle)] bg-[var(--bg-surface)] text-[11px] font-mono uppercase tracking-wider text-[var(--text-muted)]">
                        <th className="py-3 px-4">Contact</th>
                        <th className="py-3 px-4">Organization</th>
                        <th className="py-3 px-4">Channels</th>
                        <th className="py-3 px-4">Product Interest</th>
                        <th className="py-3 px-4">Message</th>
                        <th className="py-3 px-4">Date</th>
                        <th className="py-3 px-4">Status</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-[var(--border-subtle)] text-xs">
                      {inquiries.length === 0 ? (
                        <tr>
                          <td colSpan={7} className="py-8 text-center text-[var(--text-muted)]">
                            No inquiries recorded yet. Submit a test form on the Contact page.
                          </td>
                        </tr>
                      ) : (
                        inquiries.map((inq) => (
                          <tr key={inq.id} className="hover:bg-[var(--bg-card-hover)] transition-colors">
                            <td className="py-3 px-4">
                              <div className="font-bold text-[var(--text-main)]">{inq.name}</div>
                              <div className="text-[11px] text-[var(--text-muted)] font-mono">{inq.email}</div>
                            </td>
                            <td className="py-3 px-4 text-[var(--text-secondary)] font-medium">
                              {inq.company || 'Direct Broadcast Engineer'}
                            </td>
                            <td className="py-3 px-4 font-mono text-[11px]">
                              {inq.channelCount || '1-4 Channels'}
                            </td>
                            <td className="py-3 px-4">
                              <span className="px-2.5 py-0.5 rounded-full text-[10px] font-mono font-semibold bg-[var(--accent)]/10 text-[var(--accent-light)] border border-[var(--accent)]/20">
                                {inq.productInterest || 'Full Suite'}
                              </span>
                            </td>
                            <td className="py-3 px-4 max-w-xs text-[var(--text-secondary)] truncate">
                              {inq.message}
                            </td>
                            <td className="py-3 px-4 font-mono text-[10px] text-[var(--text-muted)] whitespace-nowrap">
                              {new Date(inq.createdAt).toLocaleDateString()}
                            </td>
                            <td className="py-3 px-4">
                              <span className="px-2 py-0.5 rounded text-[10px] font-mono font-semibold bg-emerald-500/10 text-emerald-400 border border-emerald-500/20">
                                {inq.status}
                              </span>
                            </td>
                          </tr>
                        ))
                      )}
                    </tbody>
                  </table>
                </div>
              </div>
            </div>
          )}

          {/* TAB 6: SYSTEM & INFRASTRUCTURE */}
          {activeSection === 'system' && (
            <div className="space-y-6">
              <div>
                <h1 className="text-2xl font-black tracking-tight">System Infrastructure & Database Engine</h1>
                <p className="text-sm text-[var(--text-muted)] mt-1">
                  Active connection parameters for local MySQL 9.1 and Prisma ORM client.
                </p>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <div className="rounded-2xl bg-[var(--bg-card)] border border-[var(--border-subtle)] p-6 space-y-4">
                  <div className="flex items-center gap-3">
                    <Database className="w-5 h-5 text-[var(--accent)]" />
                    <h3 className="font-bold text-sm">MySQL 9.1 Database Health</h3>
                  </div>
                  <div className="space-y-2 text-xs font-mono">
                    <div className="flex justify-between py-1.5 border-b border-[var(--border-subtle)]">
                      <span className="text-[var(--text-muted)]">Database Name:</span>
                      <span className="text-emerald-400 font-bold">kashtrix_web</span>
                    </div>
                    <div className="flex justify-between py-1.5 border-b border-[var(--border-subtle)]">
                      <span className="text-[var(--text-muted)]">Host:</span>
                      <span>127.0.0.1:3306</span>
                    </div>
                    <div className="flex justify-between py-1.5 border-b border-[var(--border-subtle)]">
                      <span className="text-[var(--text-muted)]">Authentication:</span>
                      <span>root (No Password)</span>
                    </div>
                    <div className="flex justify-between py-1.5 border-b border-[var(--border-subtle)]">
                      <span className="text-[var(--text-muted)]">Prisma ORM Client:</span>
                      <span className="text-indigo-400">v6.4.1 Connected</span>
                    </div>
                    <div className="flex justify-between py-1.5">
                      <span className="text-[var(--text-muted)]">Total Modules:</span>
                      <span>{products.length} registered</span>
                    </div>
                  </div>
                </div>

                <div className="rounded-2xl bg-[var(--bg-card)] border border-[var(--border-subtle)] p-6 space-y-4">
                  <div className="flex items-center gap-3">
                    <Radio className="w-5 h-5 text-emerald-400" />
                    <h3 className="font-bold text-sm">Broadcast Standards Compliance</h3>
                  </div>
                  <div className="space-y-2 text-xs font-mono">
                    <div className="flex justify-between py-1.5 border-b border-[var(--border-subtle)]">
                      <span className="text-[var(--text-muted)]">SMPTE ST 2110-20 (Video):</span>
                      <span className="text-emerald-400">PASSED (4K Uncompressed)</span>
                    </div>
                    <div className="flex justify-between py-1.5 border-b border-[var(--border-subtle)]">
                      <span className="text-[var(--text-muted)]">SMPTE ST 2022-7 (Redundancy):</span>
                      <span className="text-emerald-400">PASSED (Hitless Failover)</span>
                    </div>
                    <div className="flex justify-between py-1.5 border-b border-[var(--border-subtle)]">
                      <span className="text-[var(--text-muted)]">NDI 6 / 10-bit HDR:</span>
                      <span className="text-emerald-400">PASSED</span>
                    </div>
                    <div className="flex justify-between py-1.5 border-b border-[var(--border-subtle)]">
                      <span className="text-[var(--text-muted)]">SCTE-35 / SCTE-104 Ad Splice:</span>
                      <span className="text-emerald-400">PASSED</span>
                    </div>
                    <div className="flex justify-between py-1.5">
                      <span className="text-[var(--text-muted)]">MOS Protocol 2.8.4:</span>
                      <span className="text-emerald-400">PASSED (Port 10540)</span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          )}

        </div>
      </main>

      {/* CREATE / EDIT MODULE MODAL (Shadcn Dialog Style) */}
      {(editingProduct || isCreatingProduct) && (
        <div className="fixed inset-0 z-50 bg-black/80 backdrop-blur-md flex items-center justify-center p-4 overflow-y-auto">
          <div className="relative max-w-3xl w-full bg-[var(--bg-card)] border border-[var(--border-strong)] rounded-2xl shadow-2xl overflow-hidden my-8" onClick={(e) => e.stopPropagation()}>
            <div className="flex items-center justify-between px-6 py-4 border-b border-[var(--border-subtle)] bg-[var(--bg-surface)]">
              <div>
                <h3 className="font-bold text-base text-[var(--text-main)]">
                  {editingProduct ? `Edit Module: ${editingProduct.name}` : 'Create New Broadcast Application'}
                </h3>
                <p className="text-xs text-[var(--text-muted)]">Persists directly into the MySQL `Product` table.</p>
              </div>
              <button
                onClick={() => {
                  setEditingProduct(null);
                  setIsCreatingProduct(false);
                }}
                className="p-1.5 rounded-lg hover:bg-white/10 text-[var(--text-muted)] hover:text-white transition-colors"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            <form onSubmit={handleSaveProduct} className="p-6 space-y-4 max-h-[80vh] overflow-y-auto">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-semibold text-[var(--text-secondary)] mb-1">Module Name</label>
                  <input
                    type="text"
                    required
                    value={productForm.name}
                    onChange={(e) => setProductForm({ ...productForm, name: e.target.value })}
                    placeholder="e.g. Kashtrix Playout Engine"
                    className="w-full px-3.5 py-2 rounded-xl bg-[var(--bg-surface)] border border-[var(--border-subtle)] text-xs text-[var(--text-main)] focus:outline-none focus:border-[var(--accent)]"
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold text-[var(--text-secondary)] mb-1">URL Slug</label>
                  <input
                    type="text"
                    required
                    value={productForm.slug}
                    onChange={(e) => setProductForm({ ...productForm, slug: e.target.value })}
                    placeholder="e.g. playout"
                    className="w-full px-3.5 py-2 rounded-xl bg-[var(--bg-surface)] border border-[var(--border-subtle)] text-xs text-[var(--text-main)] focus:outline-none focus:border-[var(--accent)]"
                  />
                </div>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-semibold text-[var(--text-secondary)] mb-1">Category</label>
                  <select
                    value={productForm.category}
                    onChange={(e) => setProductForm({ ...productForm, category: e.target.value })}
                    className="w-full px-3.5 py-2 rounded-xl bg-[var(--bg-surface)] border border-[var(--border-subtle)] text-xs text-[var(--text-main)] focus:outline-none focus:border-[var(--accent)]"
                  >
                    <option value="Core Playout">Core Playout</option>
                    <option value="Live Production">Live Production</option>
                    <option value="Graphics & CG">Graphics & CG</option>
                    <option value="Asset Management">Asset Management</option>
                    <option value="Transmission & I/O">Transmission & I/O</option>
                    <option value="Monitoring & QC">Monitoring & QC</option>
                    <option value="Newsroom & MOS">Newsroom & MOS</option>
                    <option value="System & API">System & API</option>
                  </select>
                </div>
                <div>
                  <label className="block text-xs font-semibold text-[var(--text-secondary)] mb-1">Badge Tag</label>
                  <input
                    type="text"
                    value={productForm.badge}
                    onChange={(e) => setProductForm({ ...productForm, badge: e.target.value })}
                    placeholder="e.g. 24/7 AUTOMATION"
                    className="w-full px-3.5 py-2 rounded-xl bg-[var(--bg-surface)] border border-[var(--border-subtle)] text-xs text-[var(--text-main)] focus:outline-none focus:border-[var(--accent)]"
                  />
                </div>
              </div>

              <div>
                <label className="block text-xs font-semibold text-[var(--text-secondary)] mb-1">Hero Image Asset</label>
                <select
                  value={productForm.heroImage}
                  onChange={(e) => setProductForm({ ...productForm, heroImage: e.target.value })}
                  className="w-full px-3.5 py-2 rounded-xl bg-[var(--bg-surface)] border border-[var(--border-subtle)] text-xs text-[var(--text-main)] focus:outline-none focus:border-[var(--accent)]"
                >
                  {AVAILABLE_IMAGES.map((img, i) => (
                    <option key={i} value={img.path}>
                      {img.label} ({img.path})
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block text-xs font-semibold text-[var(--text-secondary)] mb-1">Tagline</label>
                <input
                  type="text"
                  value={productForm.tagline}
                  onChange={(e) => setProductForm({ ...productForm, tagline: e.target.value })}
                  placeholder="e.g. Master Control 24/7 Channel Automation with Baseband SDI & ST 2110 IP"
                  className="w-full px-3.5 py-2 rounded-xl bg-[var(--bg-surface)] border border-[var(--border-subtle)] text-xs text-[var(--text-main)] focus:outline-none focus:border-[var(--accent)]"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-[var(--text-secondary)] mb-1">Summary (1-2 Sentences)</label>
                <textarea
                  rows={2}
                  value={productForm.summary}
                  onChange={(e) => setProductForm({ ...productForm, summary: e.target.value })}
                  className="w-full px-3.5 py-2 rounded-xl bg-[var(--bg-surface)] border border-[var(--border-subtle)] text-xs text-[var(--text-main)] focus:outline-none focus:border-[var(--accent)]"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-[var(--text-secondary)] mb-1">Technical Specifications (JSON format)</label>
                <textarea
                  rows={4}
                  value={productForm.specs}
                  onChange={(e) => setProductForm({ ...productForm, specs: e.target.value })}
                  className="w-full px-3.5 py-2 rounded-xl bg-[var(--bg-surface)] border border-[var(--border-subtle)] text-xs font-mono text-[var(--text-main)] focus:outline-none focus:border-[var(--accent)]"
                />
              </div>

              <div className="pt-4 flex items-center justify-end gap-3 border-t border-[var(--border-subtle)]">
                <button
                  type="button"
                  onClick={() => {
                    setEditingProduct(null);
                    setIsCreatingProduct(false);
                  }}
                  className="px-4 py-2 rounded-xl border border-[var(--border-subtle)] text-xs font-semibold text-[var(--text-secondary)] hover:bg-[var(--bg-surface)]"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="px-5 py-2 rounded-xl bg-gradient-to-r from-[#7C3AED] to-[#A855F7] text-white text-xs font-semibold shadow-md hover:opacity-95"
                >
                  Save Module to MySQL
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* MANAGE MEDIA & FEATURES MODAL */}
      {selectedProductForMedia && (
        <div className="fixed inset-0 z-50 bg-black/80 backdrop-blur-md flex items-center justify-center p-4 overflow-y-auto">
          <div className="relative max-w-4xl w-full bg-[var(--bg-card)] border border-[var(--border-strong)] rounded-2xl shadow-2xl overflow-hidden my-8" onClick={(e) => e.stopPropagation()}>
            <div className="flex items-center justify-between px-6 py-4 border-b border-[var(--border-subtle)] bg-[var(--bg-surface)]">
              <div>
                <h3 className="font-bold text-base text-[var(--text-main)]">
                  Assets & Features: {selectedProductForMedia.name}
                </h3>
                <p className="text-xs text-[var(--text-muted)]">Attach high-resolution screenshots and feature checklists to this module.</p>
              </div>
              <button
                onClick={() => setSelectedProductForMedia(null)}
                className="p-1.5 rounded-lg hover:bg-white/10 text-[var(--text-muted)] hover:text-white transition-colors"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            <div className="p-6 space-y-8 max-h-[80vh] overflow-y-auto">
              {/* Screenshots Section */}
              <div className="space-y-4">
                <div className="flex items-center justify-between">
                  <h4 className="font-bold text-xs uppercase tracking-wider text-[var(--text-muted)] font-mono">
                    Attached Screenshots ({selectedProductForMedia.screenshots?.length || 0})
                  </h4>
                </div>

                <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-3">
                  {selectedProductForMedia.screenshots?.map((s) => (
                    <div key={s.id} className="p-2.5 rounded-xl border border-[var(--border-subtle)] bg-[var(--bg-surface)] space-y-2">
                      <div className="aspect-video rounded-lg overflow-hidden bg-black/40 cursor-pointer" onClick={() => setActiveLightboxImg(s.imagePath)}>
                        <img src={s.imagePath} alt={s.title} className="w-full h-full object-cover" />
                      </div>
                      <div className="flex items-center justify-between">
                        <div className="font-semibold text-xs truncate max-w-[150px]">{s.title}</div>
                        <button
                          onClick={() => handleDeleteScreenshot(s.id)}
                          className="p-1 rounded text-[var(--text-muted)] hover:text-red-400"
                        >
                          <Trash2 className="w-3.5 h-3.5" />
                        </button>
                      </div>
                    </div>
                  ))}
                </div>

                {/* Form to attach new screenshot */}
                <form onSubmit={handleAddScreenshot} className="p-4 rounded-xl border border-dashed border-[var(--border-strong)] bg-[var(--bg-surface)]/50 space-y-3">
                  <span className="text-xs font-bold text-[var(--text-main)] block">+ Attach Screenshot Asset</span>
                  <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
                    <input
                      type="text"
                      required
                      placeholder="Screenshot Title"
                      value={newScreenshotForm.title}
                      onChange={(e) => setNewScreenshotForm({ ...newScreenshotForm, title: e.target.value })}
                      className="px-3 py-1.5 rounded-lg bg-[var(--bg-card)] border border-[var(--border-subtle)] text-xs text-[var(--text-main)] focus:outline-none focus:border-[var(--accent)]"
                    />
                    <select
                      value={newScreenshotForm.imagePath}
                      onChange={(e) => setNewScreenshotForm({ ...newScreenshotForm, imagePath: e.target.value })}
                      className="px-3 py-1.5 rounded-lg bg-[var(--bg-card)] border border-[var(--border-subtle)] text-xs text-[var(--text-main)] focus:outline-none focus:border-[var(--accent)]"
                    >
                      {AVAILABLE_IMAGES.map((img, i) => (
                        <option key={i} value={img.path}>{img.label}</option>
                      ))}
                    </select>
                    <button
                      type="submit"
                      className="px-4 py-1.5 rounded-lg bg-[var(--accent)] text-white text-xs font-semibold hover:bg-[var(--accent-dark)]"
                    >
                      Attach Screenshot
                    </button>
                  </div>
                </form>
              </div>

              {/* Features Checklist Section */}
              <div className="space-y-4 pt-4 border-t border-[var(--border-subtle)]">
                <h4 className="font-bold text-xs uppercase tracking-wider text-[var(--text-muted)] font-mono">
                  Module Feature Highlights ({selectedProductForMedia.features?.length || 0})
                </h4>

                <div className="space-y-2">
                  {selectedProductForMedia.features?.map((f) => (
                    <div key={f.id} className="p-3 rounded-xl border border-[var(--border-subtle)] bg-[var(--bg-surface)] flex items-center justify-between">
                      <div className="flex items-center gap-3">
                        <CheckCircle2 className="w-4 h-4 text-emerald-400" />
                        <div>
                          <div className="font-semibold text-xs">{f.title}</div>
                          <div className="text-[11px] text-[var(--text-muted)]">{f.description}</div>
                        </div>
                      </div>
                      <button
                        onClick={() => handleDeleteFeature(f.id)}
                        className="p-1 rounded text-[var(--text-muted)] hover:text-red-400"
                      >
                        <Trash2 className="w-3.5 h-3.5" />
                      </button>
                    </div>
                  ))}
                </div>

                {/* Form to add feature */}
                <form onSubmit={handleAddFeature} className="p-4 rounded-xl border border-dashed border-[var(--border-strong)] bg-[var(--bg-surface)]/50 space-y-3">
                  <span className="text-xs font-bold text-[var(--text-main)] block">+ Add Feature Bullet Point</span>
                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                    <input
                      type="text"
                      required
                      placeholder="Feature Title (e.g. ST 2110 IP Core)"
                      value={newFeatureForm.title}
                      onChange={(e) => setNewFeatureForm({ ...newFeatureForm, title: e.target.value })}
                      className="px-3 py-1.5 rounded-lg bg-[var(--bg-card)] border border-[var(--border-subtle)] text-xs text-[var(--text-main)] focus:outline-none focus:border-[var(--accent)]"
                    />
                    <input
                      type="text"
                      required
                      placeholder="Feature Description"
                      value={newFeatureForm.description}
                      onChange={(e) => setNewFeatureForm({ ...newFeatureForm, description: e.target.value })}
                      className="px-3 py-1.5 rounded-lg bg-[var(--bg-card)] border border-[var(--border-subtle)] text-xs text-[var(--text-main)] focus:outline-none focus:border-[var(--accent)]"
                    />
                  </div>
                  <div className="flex justify-end">
                    <button
                      type="submit"
                      className="px-4 py-1.5 rounded-lg bg-[var(--accent)] text-white text-xs font-semibold hover:bg-[var(--accent-dark)]"
                    >
                      Add Feature
                    </button>
                  </div>
                </form>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
