// src/components/CurrentImageDisplay.tsx
import React from 'react';

interface UploadedFile {
  fileName: string;
  path: string;
  categoryId?: string;
}

interface Category {
  id: string;
  name: string;
}

interface CurrentImageDisplayProps {
  image: UploadedFile | null;
  categories: Category[];
}

const CurrentImageDisplay: React.FC<CurrentImageDisplayProps> = ({ image, categories }) => {
  if (!image) {
    return (
      <div className="h-full min-h-[400px] bg-gray-300 flex items-center justify-center text-gray-500 p-4">
        Select an image from the thumbnails below to view it here.
      </div>
    );
  }

  const getCategoryName = (categoryId: string | undefined): string | null => {
    if (!categoryId) return null;
    const category = categories.find(cat => cat.id === categoryId);
    return category ? category.name : 'Unknown Category';
  };

  const categoryName = getCategoryName(image.categoryId);

  return (
    <div className="h-full min-h-[400px] bg-gray-700 flex flex-col items-center justify-center p-4 overflow-hidden">
      <div className="flex-grow flex items-center justify-center max-h-[calc(100%-50px)] w-full"> {/* Container for image to control size */}
        <img
          src={`http://localhost:5000/Uploads/${image.fileName}`}
          alt={image.fileName}
          className="max-w-full max-h-full object-contain shadow-lg"
        />
      </div>
      <div className="mt-auto pt-2 text-center"> {/* Info below image */}
        <p className="text-white text-base font-semibold truncate max-w-full px-2">{image.fileName}</p>
        {categoryName ? (
          <p className="text-yellow-400 text-sm mt-1">Classified as: {categoryName}</p>
        ) : (
          <p className="text-gray-400 text-sm mt-1">Status: Unclassified</p>
        )}
      </div>
    </div>
  );
};
export default CurrentImageDisplay;
