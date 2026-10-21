import { NextResponse } from 'next/server';
import { prisma } from '@/lib/prisma';

export async function POST(req: Request) {
  try {
    const body = await req.json();
    const { productId, title, description, icon, order } = body;

    if (!productId || !title || !description) {
      return NextResponse.json(
        { error: 'productId, title, and description are required.' },
        { status: 400 }
      );
    }

    const feature = await prisma.productFeature.create({
      data: {
        productId: Number(productId),
        title,
        description,
        icon: icon || 'CheckCircle',
        order: Number(order) || 0
      }
    });

    return NextResponse.json({ success: true, feature }, { status: 201 });
  } catch (error: any) {
    return NextResponse.json({ error: error.message }, { status: 500 });
  }
}

export async function DELETE(req: Request) {
  try {
    const { searchParams } = new URL(req.url);
    const id = searchParams.get('id');

    if (!id) {
      return NextResponse.json({ error: 'Feature ID is required.' }, { status: 400 });
    }

    await prisma.productFeature.delete({
      where: { id: Number(id) }
    });

    return NextResponse.json({ success: true, message: 'Feature deleted.' });
  } catch (error: any) {
    return NextResponse.json({ error: error.message }, { status: 500 });
  }
}
