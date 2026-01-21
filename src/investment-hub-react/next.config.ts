import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // output: 'export', // Removed to enable Hybrid mode for dynamic routes
  // Optional: Disable image optimization if not using a Node server, 
  // as it requires 'next start' or a custom loader.
  images: {
    unoptimized: true,
  },
};

export default nextConfig;
