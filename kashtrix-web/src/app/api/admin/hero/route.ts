import { NextResponse } from 'next/server';
import { prisma } from '@/lib/prisma';

export async function GET() {
  try {
    const hero = await prisma.heroContent.findFirst();
    const settings = await prisma.siteSetting.findMany();
    return NextResponse.json({ hero, settings });
  } catch (error: any) {
    return NextResponse.json({ error: error.message }, { status: 500 });
  }
}

export async function PUT(req: Request) {
  try {
    const body = await req.json();
    const {
      badge,
      title,
      gradientTitle,
      subtitle,
      primaryCta,
      primaryCtaUrl,
      secondaryCta,
      secondaryCtaUrl,
      stats
    } = body;

    const existing = await prisma.heroContent.findFirst();

    let updated;
    if (existing) {
      updated = await prisma.heroContent.update({
        where: { id: existing.id },
        data: {
          badge,
          title,
          gradientTitle,
          subtitle,
          primaryCta,
          primaryCtaUrl,
          secondaryCta,
          secondaryCtaUrl,
          stats: typeof stats === 'string' ? stats : JSON.stringify(stats || [])
        }
      });
    } else {
      updated = await prisma.heroContent.create({
        data: {
          badge: badge || 'NEXT-GEN BROADCAST ARCHITECTURE',
          title: title || 'Enterprise TV Playout & Live Production',
          gradientTitle: gradientTitle || 'Unified in Kashtrix',
          subtitle: subtitle || 'Professional Broadcast Playout',
          primaryCta: primaryCta || 'Explore Products',
          primaryCtaUrl: primaryCtaUrl || '/products',
          secondaryCta: secondaryCta || 'Book Demo',
          secondaryCtaUrl: secondaryCtaUrl || '/contact',
          stats: typeof stats === 'string' ? stats : JSON.stringify(stats || [])
        }
      });
    }

    return NextResponse.json({ success: true, hero: updated });
  } catch (error: any) {
    return NextResponse.json({ error: error.message }, { status: 500 });
  }
}
