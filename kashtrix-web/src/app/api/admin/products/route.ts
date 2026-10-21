import { NextResponse } from 'next/server';
import { prisma } from '@/lib/prisma';

// GET all products with features and screenshots
export async function GET() {
  try {
    const products = await prisma.product.findMany({
      include: {
        features: { orderBy: { order: 'asc' } },
        screenshots: { orderBy: { order: 'asc' } }
      },
      orderBy: { order: 'asc' }
    });
    return NextResponse.json({ products });
  } catch (error: any) {
    return NextResponse.json({ error: error.message }, { status: 500 });
  }
}

// POST create a new product
export async function POST(req: Request) {
  try {
    const body = await req.json();
    const {
      name,
      slug,
      tagline,
      category,
      badge,
      heroImage,
      summary,
      description,
      icon,
      accentColor,
      specs,
      order
    } = body;

    if (!name || !slug || !category || !summary) {
      return NextResponse.json(
        { error: 'Name, slug, category, and summary are required fields.' },
        { status: 400 }
      );
    }

    const newProduct = await prisma.product.create({
      data: {
        name,
        slug: slug.toLowerCase().trim().replace(/[^a-z0-9-]+/g, '-'),
        tagline: tagline || '',
        category,
        badge: badge || 'Broadcast Module',
        heroImage: heroImage || '/assets/products/playout-master-rundown.png',
        summary,
        description: description || summary,
        icon: icon || 'Tv',
        accentColor: accentColor || '#8055B3',
        specs: typeof specs === 'string' ? specs : JSON.stringify(specs || {}),
        order: Number(order) || 0
      }
    });

    return NextResponse.json({ success: true, product: newProduct }, { status: 201 });
  } catch (error: any) {
    return NextResponse.json({ error: error.message }, { status: 500 });
  }
}

// PUT update an existing product
export async function PUT(req: Request) {
  try {
    const body = await req.json();
    const { id, ...updateData } = body;

    if (!id) {
      return NextResponse.json({ error: 'Product ID is required.' }, { status: 400 });
    }

    if (updateData.specs && typeof updateData.specs !== 'string') {
      updateData.specs = JSON.stringify(updateData.specs);
    }

    const updated = await prisma.product.update({
      where: { id: Number(id) },
      data: updateData
    });

    return NextResponse.json({ success: true, product: updated });
  } catch (error: any) {
    return NextResponse.json({ error: error.message }, { status: 500 });
  }
}

// DELETE a product
export async function DELETE(req: Request) {
  try {
    const { searchParams } = new URL(req.url);
    const id = searchParams.get('id');

    if (!id) {
      return NextResponse.json({ error: 'Product ID is required.' }, { status: 400 });
    }

    await prisma.product.delete({
      where: { id: Number(id) }
    });

    return NextResponse.json({ success: true, message: 'Product deleted successfully.' });
  } catch (error: any) {
    return NextResponse.json({ error: error.message }, { status: 500 });
  }
}
