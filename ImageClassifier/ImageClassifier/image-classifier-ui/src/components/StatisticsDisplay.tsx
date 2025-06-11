// src/components/StatisticsDisplay.tsx
import React, { useState, useMemo } from 'react';

interface CategoryStat {
  categoryId: string;
  categoryName: string;
  count: number;
  percentage: number;
}

interface AllStats {
  totalImages: number;
  classifiedImagesCount: number;
  unclassifiedImagesCount: number;
  categoryDetails: CategoryStat[];
}

interface StatisticsDisplayProps {
  stats: AllStats;
}

type SortKey = 'name' | 'count' | 'percentage';
type SortOrder = 'asc' | 'desc';

const StatisticsDisplay: React.FC<StatisticsDisplayProps> = ({ stats }) => {
  const [sortKey, setSortKey] = useState<SortKey>('name');
  const [sortOrder, setSortOrder] = useState<SortOrder>('asc');

  const sortedCategoryDetails = useMemo(() => {
    const sorted = [...stats.categoryDetails].sort((a, b) => {
      if (sortKey === 'name') {
        return a.categoryName.localeCompare(b.categoryName);
      } else if (sortKey === 'count') {
        return a.count - b.count;
      } else { // percentage
        return a.percentage - b.percentage;
      }
    });
    return sortOrder === 'asc' ? sorted : sorted.reverse();
  }, [stats.categoryDetails, sortKey, sortOrder]);

  const handleSort = (key: SortKey) => {
    if (sortKey === key) {
      setSortOrder(prevOrder => prevOrder === 'asc' ? 'desc' : 'asc');
    } else {
      setSortKey(key);
      setSortOrder('asc');
    }
  };

  const getSortIndicator = (key: SortKey) => {
    if (sortKey !== key) return '';
    return sortOrder === 'asc' ? ' ▲' : ' ▼';
  };

  return (
    <div className="bg-gray-50 p-4 w-[300px] h-full text-sm">
      <h3 className="text-lg font-semibold mb-4 text-gray-800 border-b pb-2">Statistics</h3>

      <div className="space-y-1 mb-4">
        <p>Total Images: <span className="font-medium float-right">{stats.totalImages}</span></p>
        <p>Classified: <span className="font-medium float-right">{stats.classifiedImagesCount}</span></p>
        <p>Unclassified: <span className="font-medium float-right">{stats.unclassifiedImagesCount}</span></p>
      </div>

      <h4 className="text-md font-semibold mt-4 mb-2 text-gray-700">By Category:</h4>
      {stats.categoryDetails.length === 0 ? (
        <p className="text-gray-500">No categories or no classified images yet.</p>
      ) : (
        <table className="w-full table-auto border-collapse">
          <thead>
            <tr className="bg-gray-200">
              <th className="px-2 py-1 text-left text-xs font-medium text-gray-600 uppercase tracking-wider cursor-pointer hover:bg-gray-300" onClick={() => handleSort('name')}>
                Name{getSortIndicator('name')}
              </th>
              <th className="px-2 py-1 text-right text-xs font-medium text-gray-600 uppercase tracking-wider cursor-pointer hover:bg-gray-300" onClick={() => handleSort('count')}>
                # {getSortIndicator('count')}
              </th>
              <th className="px-2 py-1 text-right text-xs font-medium text-gray-600 uppercase tracking-wider cursor-pointer hover:bg-gray-300" onClick={() => handleSort('percentage')}>
                % {getSortIndicator('percentage')}
              </th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {sortedCategoryDetails.map(cat => (
              <tr key={cat.categoryId}>
                <td className="px-2 py-1 whitespace-nowrap text-gray-700">{cat.categoryName}</td>
                <td className="px-2 py-1 whitespace-nowrap text-right text-gray-700">{cat.count}</td>
                <td className="px-2 py-1 whitespace-nowrap text-right text-gray-700">{cat.percentage.toFixed(1)}%</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
};

export default StatisticsDisplay;
