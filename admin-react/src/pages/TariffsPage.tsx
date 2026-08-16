import { useEffect, useState } from 'react';
import api from '../api/axiosConfig';

export interface Tariff { id: string; name: string; price: number; type: string; }

export const TariffsPage = () => {
  const [tariffs, setTariffs] = useState<Tariff[]>([]);

  useEffect(() => {
    api.get('/billing/tariffs').then((res: any) => {
      const data = res.data;
      // Handle Result<T> wrapper
      if (data?.isSuccess) {
        setTariffs(data.value || []);
      }
    });
  }, []);

  return (
    <div className="p-10">
      <h1 className="text-2xl font-bold mb-5">Тарифы</h1>
      <div className="grid grid-cols-3 gap-4">
        {tariffs.map(t => (
          <div key={t.id} className="p-4 bg-white shadow rounded">
            <h3 className="font-bold">{t.name}</h3>
            <p>{t.price} центов</p>
          </div>
        ))}
      </div>
    </div>
  );
};
