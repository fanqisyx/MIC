// src/components/ImageUploader.tsx
import React, { useState, ChangeEvent } from 'react';

interface ImageUploaderProps {
    onUploadSuccess: (uploadedFiles: UploadedFile[]) => void;
}
// Define UploadedFile if not already defined globally or imported
interface UploadedFile {
    fileName: string;
    size: number;
    path: string; // Server path
}


const ImageUploader: React.FC<ImageUploaderProps> = ({ onUploadSuccess }) => {
    const [selectedFiles, setSelectedFiles] = useState<FileList | null>(null);
    const [message, setMessage] = useState<string>('');
    const [error, setError] = useState<string>('');
    const [uploading, setUploading] = useState<boolean>(false);

    const handleFileChange = (event: ChangeEvent<HTMLInputElement>) => {
        if (event.target.files) {
            setSelectedFiles(event.target.files);
            setMessage('');
            setError('');
        }
    };

    const handleUpload = async () => {
        if (!selectedFiles || selectedFiles.length === 0) {
            setError('Please select files to upload.');
            return;
        }

        const formData = new FormData();
        for (let i = 0; i < selectedFiles.length; i++) {
            formData.append('files', selectedFiles[i]);
        }

        setUploading(true);
        setMessage('');
        setError('');

        try {
            // Adjust the URL to where your backend API is running
            const response = await fetch('http://localhost:5000/api/images/upload', { // Ensure this port matches your .NET API's port
                method: 'POST',
                body: formData,
                // Headers are not typically needed for FormData with fetch,
                // as the browser sets 'Content-Type': 'multipart/form-data' automatically.
            });

            const result = await response.json();

            if (!response.ok) {
                throw new Error(result.message || `Server error: ${response.status}`);
            }

            setMessage(result.message || 'Files uploaded successfully!');
            if (result.files && result.files.length > 0) {
                onUploadSuccess(result.files); // Pass data up
            }
            // console.log('Uploaded files data:', result.files); // Keep for debugging if needed

        } catch (err: any) {
            setError(err.message || 'Upload failed. Please try again.');
            console.error('Upload error:', err);
        } finally {
            setUploading(false);
            setSelectedFiles(null); // Clear selection after upload attempt
            // Clear the file input visually (this is a common trick)
            const fileInput = document.getElementById('fileInput') as HTMLInputElement;
            if (fileInput) {
                fileInput.value = '';
            }
        }
    };

    return (
        <div className="p-4 border rounded-lg shadow-md">
            <h2 className="text-xl font-semibold mb-4">Upload Images</h2>
            <div className="mb-4">
                <input
                    id="fileInput"
                    type="file"
                    multiple
                    accept="image/jpeg, image/png"
                    onChange={handleFileChange}
                    className="block w-full text-sm text-slate-500
                               file:mr-4 file:py-2 file:px-4
                               file:rounded-full file:border-0
                               file:text-sm file:font-semibold
                               file:bg-violet-50 file:text-violet-700
                               hover:file:bg-violet-100"
                />
            </div>
            <button
                onClick={handleUpload}
                disabled={uploading || !selectedFiles || selectedFiles.length === 0}
                className="px-4 py-2 bg-blue-500 text-white rounded hover:bg-blue-600 disabled:bg-gray-300"
            >
                {uploading ? 'Uploading...' : 'Upload Selected'}
            </button>
            {message && <p className="mt-4 text-green-600">{message}</p>}
            {error && <p className="mt-4 text-red-600">{error}</p>}
        </div>
    );
};

export default ImageUploader;
