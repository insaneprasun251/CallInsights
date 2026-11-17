export class ApiError extends Error {
  constructor(public status: number, message: string) {
    super(message);
  }
}

async function readMessage(res: Response): Promise<string> {
  if (res.status === 413) return 'The file is too large.';
  try {
    const body = await res.json();
    return body.detail ?? body.title ?? `Request failed (${res.status})`;
  } catch {
    return `Request failed (${res.status})`;
  }
}

export async function apiRequest<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`/api${path}`, init);
  if (!res.ok) throw new ApiError(res.status, await readMessage(res));

  const text = await res.text();
  return (text ? JSON.parse(text) : undefined) as T;
}