import { useEffect, useState } from 'react';
import { billingApi } from '../api/axiosConfig';

export interface Tariff {
  id: string;
  name: string;
  type: string;
  pricePerMinuteCents: number;
  pricePerHourCents: number;
  isActive: boolean;
}

export const TariffsPage = () => {
  const [tariffs, setTariffs] = useState<Tariff[]>([]);

  useEffect(() => {
    billingApi.get('/billing/tariffs').then((res) => {
      const data = res.data;
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
            <p>{t.pricePerHourCents} центов/час</p>
          </div>
        ))}
      </div>
    </div>
  );
};
