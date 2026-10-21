import { NextResponse } from 'next/server';
import { prisma } from '@/lib/prisma';

export async function POST(req: Request) {
  try {
    const body = await req.json();
    const { name, email, company, channelCount, productInterest, message } = body;

    if (!name || !email || !message) {
      return NextResponse.json(
        { error: 'Name, email, and message are required.' },
        { status: 400 }
      );
    }

    const inquiry = await prisma.inquiry.create({
      data: {
        name,
        email,
        company: company || '',
        channelCount: channelCount || '1-4 Channels',
        productInterest: productInterest || 'Complete Broadcast Suite',
        message
      }
    });

    return NextResponse.json(
      { success: true, message: 'Inquiry received successfully!', inquiry },
      { status: 201 }
    );
  } catch (error: any) {
    console.error('Error saving inquiry:', error);
    return NextResponse.json(
      { error: error?.message || 'Internal Server Error' },
      { status: 500 }
    );
  }
}

export async function GET() {
  try {
    const inquiries = await prisma.inquiry.findMany({
      orderBy: { createdAt: 'desc' }
    });
    return NextResponse.json({ inquiries });
  } catch (error: any) {
    return NextResponse.json({ error: error.message }, { status: 500 });
  }
}
