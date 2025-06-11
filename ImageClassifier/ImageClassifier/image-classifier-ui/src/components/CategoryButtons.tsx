// src/components/CategoryButtons.tsx
import React from 'react';

interface Category {
  id: string;
  name: string;
}

interface CategoryButtonsProps {
  categories: Category[];
  onCategorize: (categoryId: string) => void;
}

const CategoryButtons: React.FC<CategoryButtonsProps> = ({ categories, onCategorize }) => {
  if (!categories || categories.length === 0) {
    return <div className="p-4 text-center text-gray-500">No categories defined. Go to settings.</div>;
  }
  return (
    <div className="bg-gray-100 p-4 h-[100px] flex items-center justify-center space-x-2 overflow-x-auto">
      <h3 className="text-sm font-semibold mr-2 text-gray-700">Categories:</h3>
      {categories.map((category) => (
        <button
          key={category.id}
          onClick={() => onCategorize(category.id)}
          className="px-4 py-2 bg-blue-500 text-white rounded hover:bg-blue-600 whitespace-nowrap"
        >
          {category.name}
        </button>
      ))}
    </div>
  );
};
export default CategoryButtons;
