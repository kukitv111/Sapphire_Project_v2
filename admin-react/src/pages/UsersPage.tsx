import { useEffect, useState } from 'react';
import api from '../api/axiosConfig';

interface User { id: string; username: string; email: string; status: string; }
interface ResultResponse<T> { isSuccess: boolean; value?: T; }

export const UsersPage = () => {
  const [users, setUsers] = useState<User[]>([]);

  useEffect(() => {
    api.get('/users').then((res: any) => {
      if (res.data?.isSuccess) setUsers(res.data.value || []);
    });
  }, []);

  return (
    <div className="p-10">
      <h1 className="text-2xl font-bold mb-5">Пользователи</h1>
      <table className="w-full bg-white shadow rounded">
        <thead><tr className="border-b"><th className="p-3">ID</th><th className="p-3">Login</th><th className="p-3">Email</th></tr></thead>
        <tbody>{users.map(u => <tr key={u.id} className="border-b"><td className="p-3">{u.id}</td><td className="p-3">{u.username}</td><td className="p-3">{u.email}</td></tr>)}</tbody>
      </table>
    </div>
  );
};
