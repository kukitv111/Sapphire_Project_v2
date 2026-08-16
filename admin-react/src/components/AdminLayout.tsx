import { Outlet, Link } from 'react-router-dom';

export const AdminLayout = () => (
  <div className="flex h-screen bg-gray-50">
    <aside className="w-64 bg-gray-900 text-white p-5">
      <h1 className="text-xl font-bold mb-8">Sapphire Admin</h1>
      <nav className="flex flex-col gap-4">
        <Link to="/dashboard" className="hover:text-blue-400">Dashboard</Link>
        <Link to="/users" className="hover:text-blue-400">Пользователи</Link>
        <Link to="/tariffs" className="hover:text-blue-400">Тарифы</Link>
        <Link to="/sessions" className="hover:text-blue-400">Сеансы</Link>
      </nav>
      <button 
        onClick={() => { localStorage.clear(); window.location.href = '/login'; }}
        className="mt-10 text-red-400"
      >
        Выйти
      </button>
    </aside>
    <main className="flex-1 overflow-y-auto">
      <Outlet />
    </main>
  </div>
);
