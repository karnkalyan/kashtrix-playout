import React from 'react';
import * as LucideIcons from 'lucide-react';

interface IconProps extends React.SVGProps<SVGSVGElement> {
  name: string;
  size?: number | string;
  className?: string;
}

export function Icon({ name, size = 20, className = '', ...props }: IconProps) {
  const iconsMap = LucideIcons as unknown as Record<string, React.ElementType>;
  const IconComponent = iconsMap[name] || LucideIcons.Tv;
  return <IconComponent size={size} className={className} {...props} />;
}
