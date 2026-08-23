import { useEffect, useState } from 'react';
import { sessionApi } from '../api/axiosConfig';

export interface Session {
  id: string;
  userId: string;
  computerId: string;
  startTime: string;
  endTime?: string | null;
  status: string;
}

export const SessionsPage = () => {
  const [sessions, setSessions] = useState<Session[]>([]);

  useEffect(() => {
    sessionApi.get('/sessions').then((res) => {
      const data = res.data;
      if (data?.isSuccess) {
        setSessions(data.value || []);
      }
    });
  }, []);

  return (
    <div className="p-10">
      <h1 className="text-2xl font-bold mb-5">Сеансы</h1>
      <table className="w-full bg-white shadow rounded">
        <thead><tr className="border-b"><th className="p-3">ID</th><th className="p-3">User</th><th className="p-3">Status</th></tr></thead>
        <tbody>{sessions.map(s => <tr key={s.id} className="border-b"><td className="p-3">{s.id}</td><td className="p-3">{s.userId}</td><td className="p-3">{s.status}</td></tr>)}</tbody>
      </table>
    </div>
  );
};
