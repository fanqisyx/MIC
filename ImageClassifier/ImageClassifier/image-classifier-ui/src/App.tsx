// src/App.tsx
import React, { useState, useEffect } from 'react';
import './App.css';
import ImageUploader from './components/ImageUploader';
import CategorySettings from './components/CategorySettings';
import ThumbnailStrip from './components/ThumbnailStrip';
import CurrentImageDisplay from './components/CurrentImageDisplay';
import CategoryButtons from './components/CategoryButtons';
import StatisticsDisplay from './components/StatisticsDisplay';
import ReportGenerator from './components/ReportGenerator';

interface UploadedFile {
  fileName: string;
  size: number;
  path: string; // Server path
  categoryId?: string; // Optional: ID of the category it's classified under
  classifiedAt?: string; // Optional: Timestamp of classification
}

interface ImageClassificationData { // For data from backend
    imageIdentifier: string;
    categoryId: string;
    classifiedAt: string;
}

interface Category { // Define or import this
  id: string;
  name: string;
}

const API_CATEGORIES_URL = '/api/categories';

function App() {
  const [uploadedImages, setUploadedImages] = useState<UploadedFile[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [selectedImage, setSelectedImage] = useState<UploadedFile | null>(null);
  // Basic stats state for now
  // const [classifiedImagesCount, setClassifiedImagesCount] = useState<number>(0); // REMOVED THIS LINE

  const [stats, setStats] = useState<AllStats>({
    totalImages: 0,
    classifiedImagesCount: 0,
    unclassifiedImagesCount: 0,
    categoryDetails: [],
  });

  // Fetch categories
  useEffect(() => {
    const fetchCategories = async () => {
      try {
        const response = await fetch(API_CATEGORIES_URL);
        if (!response.ok) throw new Error('Failed to fetch categories');
        const data: Category[] = await response.json();
        setCategories(data);
      } catch (error) {
        console.error("Error fetching categories:", error);
        // Optionally set an error state to display to the user
      }
    };
    fetchCategories();
  }, []); // Runs once on mount

  const handleUploadSuccess = (newlyUploadedFiles: UploadedFile[]) => {
    // Prepend new files to existing ones, or replace. For now, let's append.
    setUploadedImages(prevImages => [...prevImages, ...newlyUploadedFiles]);
    if (newlyUploadedFiles.length > 0 && !selectedImage) {
         setSelectedImage(newlyUploadedFiles[0]); // Auto-select first uploaded image
    }
  };

  const handleSelectImage = (image: UploadedFile) => {
    setSelectedImage(image);
  };

  // Update handleCategorizeImage
  const handleCategorizeImage = async (categoryId: string) => {
      if (!selectedImage) {
          alert("Please select an image first.");
          return;
      }

      try {
          const response = await fetch('/api/classifications', {
              method: 'POST',
              headers: { 'Content-Type': 'application/json' },
              body: JSON.stringify({
                  imageIdentifier: selectedImage.fileName,
                  categoryId: categoryId,
              }),
          });

          if (!response.ok) {
              const errorData = await response.json().catch(() => ({ message: `Server error ${response.status}`}));
              throw new Error(errorData.message || 'Failed to save classification.');
          }

          const classificationResult: ImageClassificationData = await response.json();

          // Update local state
          const updateImageState = (image: UploadedFile) => ({
              ...image,
              categoryId: classificationResult.categoryId,
              classifiedAt: classificationResult.classifiedAt,
          });

          setUploadedImages(prevImages =>
              prevImages.map(img =>
                  img.fileName === selectedImage.fileName ? updateImageState(img) : img
              )
          );

          setSelectedImage(prevSelected =>
              prevSelected && prevSelected.fileName === selectedImage.fileName
                  ? updateImageState(prevSelected)
                  : prevSelected
          );

          // Update stats (this might become more sophisticated later)
          // Recalculate classified count based on actual data rather than just incrementing
          // setClassifiedImagesCount(prevImages => prevImages.filter(img => img.categoryId).length);
          // This line is removed as there's a dedicated useEffect for classifiedImagesCount

          alert(`Image ${selectedImage.fileName} classified as category ID ${categoryId}.`);

      } catch (error: any) {
          console.error('Error classifying image:', error);
          alert(`Error: ${error.message}`);
      }
  };

  // When fetching categories (existing useEffect):
  // useEffect(() => { ... fetchCategories ... }, []);

  // New useEffect to merge classifications when images are loaded/changed
  useEffect(() => {
      const mergeClassifications = async () => {
          if (uploadedImages.length === 0) return; // No images to classify

          try {
              const response = await fetch('/api/classifications');
              if (!response.ok) {
                  console.error('Failed to fetch all classifications during merge');
                  // Optionally, handle this error more gracefully in the UI
                  return;
              }
              const allClassifications: ImageClassificationData[] = await response.json();

              if (allClassifications.length === 0) return; // No classifications to merge

              setUploadedImages(prevImages =>
                  prevImages.map(img => {
                      const foundClassification = allClassifications.find(c => c.imageIdentifier === img.fileName);
                      if (foundClassification && !img.categoryId) { // Merge if not already classified locally
                          return {
                              ...img,
                              categoryId: foundClassification.categoryId,
                              classifiedAt: foundClassification.classifiedAt,
                          };
                      }
                      return img;
                  })
              );
          } catch (error) {
              console.error("Error merging classifications:", error);
          }
      };

      mergeClassifications();
  }, [uploadedImages.length]); // Rerun if the number of images changes (e.g. after upload)
                               // This is a common pattern, but if uploadedImages objects themselves change
                               // for other reasons, this might run too often.
                               // A more specific trigger or a deep comparison (e.g. JSON.stringify) might be needed
                               // for complex scenarios, but for now, length change is a decent proxy for new uploads.


  // In App.tsx, adjust the classifiedImagesCount initialization and update logic
  // useEffect(() => {
      // This effect recalculates classified count whenever uploadedImages changes
      // setClassifiedImagesCount(uploadedImages.filter(img => img.categoryId).length); // Replaced by comprehensive stats
  // }, [uploadedImages]); // This entire useEffect is replaced by the one below


  // Effect to recalculate statistics
  useEffect(() => {
    const newTotalImages = uploadedImages.length;
    const newClassifiedImages = uploadedImages.filter(img => img.categoryId).length;
    const newUnclassifiedImages = newTotalImages - newClassifiedImages;

    const categoryCounts: { [key: string]: number } = {};
    uploadedImages.forEach(image => {
      if (image.categoryId) {
        categoryCounts[image.categoryId] = (categoryCounts[image.categoryId] || 0) + 1;
      }
    });

    const newCategoryDetails: CategoryStat[] = categories.map(category => {
      const count = categoryCounts[category.id] || 0;
      return {
        categoryId: category.id,
        categoryName: category.name,
        count: count,
        percentage: newTotalImages > 0 ? (count / newTotalImages) * 100 : 0,
      };
    });

    setStats({
      totalImages: newTotalImages,
      classifiedImagesCount: newClassifiedImages,
      unclassifiedImagesCount: newUnclassifiedImages,
      categoryDetails: newCategoryDetails,
    });

  }, [uploadedImages, categories]); // Recalculate when images or categories change

  // Basic setup for serving images from backend 'Uploads'
  // Ensure your .NET backend is configured to serve static files from 'Uploads'
  // E.g., in Program.cs: app.UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.ContentRootPath, "Uploads")), RequestPath = "/Uploads" });

  return (
    <div className="flex flex-col h-screen">
      {/* Admin/Setup sections (can be conditionally rendered or moved to routes) */}
      <div className="p-4 bg-slate-50 border-b">
        <h1 className="text-2xl font-bold text-center text-gray-700 mb-4">Image Classification Tool - Admin</h1>
        <div className="flex flex-wrap justify-around gap-4">
          <section id="image-upload-section" className="flex-1 min-w-[300px] max-w-md">
            <ImageUploader onUploadSuccess={handleUploadSuccess} />
          </section>
          <section id="category-settings-section" className="flex-1 min-w-[300px] max-w-md">
            <CategorySettings />
          </section>
        </div>
      </div>

      <hr className="my-2 border-gray-400" />

      {/* Main Classification UI */}
      <h2 className="text-xl font-semibold text-center text-gray-700 my-2">Classification View</h2>
      <div className="flex flex-1 overflow-hidden">
        {/* Left Content Area (Thumbnails, Current Image, Category Buttons) */}
        <div className="flex-1 flex flex-col overflow-hidden">
          {/* Top: Thumbnail Strip */}
          <div className="shrink-0"> {/* Prevent this from shrinking too much */}
            <ThumbnailStrip images={uploadedImages} categories={categories} selectedImage={selectedImage} onSelectImage={handleSelectImage} />
          </div>

          {/* Middle: Current Image Display */}
          <div className="flex-1 bg-gray-300 overflow-hidden"> {/* Allow this to take remaining space and handle overflow if image is too big */}
            <CurrentImageDisplay image={selectedImage} categories={categories} />
          </div>

          {/* Bottom: Category Buttons */}
          <div className="shrink-0"> {/* Prevent this from shrinking too much */}
            <CategoryButtons categories={categories} onCategorize={handleCategorizeImage} />
          </div>
        </div>

        {/* Right: Statistics Display */}
        <aside className="w-[300px] shrink-0 border-l border-gray-300 bg-gray-50 overflow-y-auto p-4 space-y-4">
                {/* Pass the new comprehensive 'stats' object */}
                <StatisticsDisplay stats={stats} />
                <hr /> {/* Optional separator */}
                <ReportGenerator />
        </aside>
      </div>
    </div>
  );
}

export default App;
