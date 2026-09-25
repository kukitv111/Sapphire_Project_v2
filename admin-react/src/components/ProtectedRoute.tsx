import { Navigate, Outlet } from 'react-router-dom';

export const ProtectedRoute = () => {
  const token = localStorage.getItem('accessToken');
  if (!token) return <Navigate to="/login" replace />;
  try {
    const user = JSON.parse(localStorage.getItem('user') || '{}');
    if (user.mustChangePassword) return <Navigate to="/change-password" replace />;
  } catch {
    return <Navigate to="/login" replace />;
  }
  return <Outlet />;
};
