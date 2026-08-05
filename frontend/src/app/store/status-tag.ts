/** Maps an order status to its Nocturne tag class (mirrors the mockup's tagStyleFor). */
export function statusTagClass(status: string): string {
  switch (status) {
    case 'Delivered': return 'tag-accent';
    case 'Shipped': return 'tag-neutral';
    case 'Processing': return 'tag-outline';
    case 'PendingPayment': return 'tag-neutral';
    default: return 'tag-dim'; // Cancelled
  }
}
