import { NextResponse } from 'next/server';
import { prisma } from '@/lib/prisma';

export async function POST(req: Request) {
  try {
    const body = await req.json();
    const { productId, title, caption, imagePath, category, order } = body;

    if (!productId || !title || !imagePath) {
      return NextResponse.json(
        { error: 'productId, title, and imagePath are required.' },
        { status: 400 }
      );
    }

    const screenshot = await prisma.productScreenshot.create({
      data: {
        productId: Number(productId),
        title,
        caption: caption || '',
        imagePath,
        category: category || 'Interface',
        order: Number(order) || 0
      }
    });

    return NextResponse.json({ success: true, screenshot }, { status: 201 });
  } catch (error: any) {
    return NextResponse.json({ error: error.message }, { status: 500 });
  }
}

export async function DELETE(req: Request) {
  try {
    const { searchParams } = new URL(req.url);
    const id = searchParams.get('id');

    if (!id) {
      return NextResponse.json({ error: 'Screenshot ID is required.' }, { status: 400 });
    }

    await prisma.productScreenshot.delete({
      where: { id: Number(id) }
    });

    return NextResponse.json({ success: true, message: 'Screenshot deleted.' });
  } catch (error: any) {
    return NextResponse.json({ error: error.message }, { status: 500 });
  }
}
