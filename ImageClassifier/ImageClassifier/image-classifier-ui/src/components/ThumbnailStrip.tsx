// src/components/ThumbnailStrip.tsx
import React from 'react';

interface UploadedFile {
  fileName: string;
  path: string; // Server path, may need to be mapped to a URL
  categoryId?: string;
}

interface Category {
  id: string;
  name: string;
}

interface ThumbnailStripProps {
  images: UploadedFile[];
  categories: Category[];
  selectedImage: UploadedFile | null;
  onSelectImage: (image: UploadedFile) => void;
}

const ThumbnailStrip: React.FC<ThumbnailStripProps> = ({ images, categories, selectedImage, onSelectImage }) => {
  if (!images || images.length === 0) {
    return <div className="p-4 text-center text-gray-500 bg-gray-200 h-[150px] flex items-center justify-center">No images loaded yet. Upload some!</div>;
  }

  const getCategoryName = (categoryId: string | undefined): string | null => {
    if (!categoryId) return null;
    const category = categories.find(cat => cat.id === categoryId);
    return category ? category.name : 'Unknown';
  };

  return (
    <div className="bg-gray-200 p-2 h-[150px] overflow-x-auto whitespace-nowrap">
      <div className="flex space-x-2">
        {images.map((image) => {
          const categoryName = getCategoryName(image.categoryId);
          const isSelected = selectedImage?.fileName === image.fileName;

          return (
            <div
              key={image.fileName} // Assuming fileName is unique for key
              className={`w-28 h-28 flex-shrink-0 cursor-pointer group relative border-4 hover:border-blue-600
                          ${isSelected ? 'border-blue-500 shadow-lg' : 'border-transparent'}`}
              onClick={() => onSelectImage(image)}
              title={image.fileName}
            >
              <img
                src={`/Uploads/${image.fileName}`}
                alt={image.fileName}
                className="w-full h-full object-cover"
              />

              {categoryName && (
                <div className="absolute bottom-0 left-0 right-0 bg-black bg-opacity-70 text-white text-xs p-1 text-center truncate">
                  {categoryName}
                </div>
              )}
              {!categoryName && isSelected && ( // Show "Unclassified" only if selected and no category
                <div className="absolute top-1 right-1 bg-gray-500 bg-opacity-70 text-white text-xs p-0.5 rounded-sm">
                  Unclassified
                </div>
              )}
               {!categoryName && !isSelected && ( // More subtle indication for unclassified & not selected
                <div className="absolute top-1 right-1 bg-gray-400 bg-opacity-50 text-white text-xs px-1 py-0.5 rounded-sm opacity-60 group-hover:opacity-100">
                  U
                </div>
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
};
export default ThumbnailStrip;
