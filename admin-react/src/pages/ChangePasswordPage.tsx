import { useState } from 'react';
import { Navigate, useNavigate } from 'react-router-dom';
import { authService } from '../api/authService';

export const ChangePasswordPage = () => {
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [error, setError] = useState('');
  const navigate = useNavigate();
  if (!localStorage.getItem('accessToken')) return <Navigate to="/login" replace />;

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    try {
      await authService.changePassword(currentPassword, newPassword);
      navigate('/login', { replace: true });
    } catch (failure) {
      setError(failure instanceof Error ? failure.message : 'Не удалось сменить пароль');
    }
  };

  return (
    <main className="flex justify-center items-center min-h-screen bg-gray-100">
      <form onSubmit={submit} className="p-8 bg-white shadow-md rounded w-full max-w-md">
        <h1 className="mb-4 text-2xl font-bold">Смена временного пароля</h1>
        <p className="mb-4">Перед началом работы задайте новый пароль.</p>
        <input className="block w-full p-2 mb-4 border" type="password"
          autoComplete="current-password" required placeholder="Текущий пароль"
          value={currentPassword} onChange={event => setCurrentPassword(event.target.value)} />
        <input className="block w-full p-2 mb-4 border" type="password"
          autoComplete="new-password" required minLength={8} placeholder="Новый пароль"
          value={newPassword} onChange={event => setNewPassword(event.target.value)} />
        {error && <p className="mb-4 text-red-700" role="alert">{error}</p>}
        <button className="w-full p-2 bg-blue-600 text-white rounded" type="submit">Сменить пароль</button>
      </form>
    </main>
  );
};
