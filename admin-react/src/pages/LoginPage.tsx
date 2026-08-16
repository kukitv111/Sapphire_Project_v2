import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { authService } from '../api/authService';

export const LoginPage = () => {
  const [login, setLogin] = useState('');
  const [password, setPassword] = useState('');
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await authService.login({ login, password });
      navigate('/dashboard');
    } catch (err) {
      alert('Ошибка входа');
    }
  };

  return (
    <div className="flex justify-center items-center h-screen bg-gray-100">
      <form onSubmit={handleSubmit} className="p-8 bg-white shadow-md rounded">
        <h2 className="mb-4 text-2xl font-bold">Вход в Admin Panel</h2>
        <input 
          className="block w-full p-2 mb-4 border"
          placeholder="Login" 
          onChange={(e) => setLogin(e.target.value)} 
        />
        <input 
          className="block w-full p-2 mb-4 border"
          type="password"
          placeholder="Password" 
          onChange={(e) => setPassword(e.target.value)} 
        />
        <button className="w-full p-2 bg-blue-600 text-white rounded">Войти</button>
      </form>
    </div>
  );
};
