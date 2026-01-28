import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  output: 'standalone', // For Azure SWA hybrid rendering
  images: {
    unoptimized: true,
  },
};

export default nextConfig;
