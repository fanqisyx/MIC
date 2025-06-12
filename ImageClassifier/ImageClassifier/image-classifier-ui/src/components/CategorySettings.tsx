// src/components/CategorySettings.tsx
import React, { useState, useEffect, ChangeEvent, FormEvent } from 'react';

interface Category {
  id: string;
  name: string;
}

const API_URL = '/api/categories'; // Adjust port if needed

const CategorySettings: React.FC = () => {
    const [categories, setCategories] = useState<Category[]>([]);
    const [isLoading, setIsLoading] = useState<boolean>(true);
    const [error, setError] = useState<string | null>(null);

    const [newCategoryName, setNewCategoryName] = useState<string>('');
    const [editingCategory, setEditingCategory] = useState<Category | null>(null);
    const [editName, setEditName] = useState<string>('');

    // Fetch categories
    const fetchCategories = async () => {
        setIsLoading(true);
        try {
            const response = await fetch(API_URL);
            if (!response.ok) {
                const errorData = await response.json().catch(() => ({ message: `Error ${response.status}` }));
                throw new Error(errorData.message || `Failed to fetch categories: ${response.statusText}`);
            }
            const data: Category[] = await response.json();
            setCategories(data);
            setError(null);
        } catch (err: any) {
            setError(err.message);
            setCategories([]); // Clear categories on error
        } finally {
            setIsLoading(false);
        }
    };

    useEffect(() => {
        fetchCategories();
    }, []);

    // Add category
    const handleAddCategory = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        if (!newCategoryName.trim()) {
            alert('Category name cannot be empty.');
            return;
        }
        try {
            const response = await fetch(API_URL, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ name: newCategoryName }),
            });
            if (!response.ok) {
                const errorData = await response.json().catch(() => ({ message: `Error ${response.status}` }));
                throw new Error(errorData.message || `Failed to add category: ${response.statusText}`);
            }
            setNewCategoryName('');
            fetchCategories(); // Refresh list
        } catch (err: any) {
            alert(`Error adding category: ${err.message}`);
        }
    };

    // Delete category
    const handleDeleteCategory = async (id: string) => {
        if (!window.confirm('Are you sure you want to delete this category?')) return;
        try {
            const response = await fetch(`${API_URL}/${id}`, { method: 'DELETE' });
            if (!response.ok) {
                 const errorData = await response.json().catch(() => ({ message: `Error ${response.status}` }));
                throw new Error(errorData.message || `Failed to delete category: ${response.statusText}`);
            }
            fetchCategories(); // Refresh list
        } catch (err: any) {
            alert(`Error deleting category: ${err.message}`);
        }
    };

    // Start editing
    const handleEditCategory = (category: Category) => {
        setEditingCategory(category);
        setEditName(category.name);
    };

    // Save edited category
    const handleSaveEdit = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        if (!editingCategory || !editName.trim()) {
             alert('Category name cannot be empty.');
            return;
        }
        try {
            const response = await fetch(`${API_URL}/${editingCategory.id}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ name: editName }),
            });
            if (!response.ok) {
                const errorData = await response.json().catch(() => ({ message: `Error ${response.status}` }));
                throw new Error(errorData.message || `Failed to update category: ${response.statusText}`);
            }
            setEditingCategory(null);
            setEditName('');
            fetchCategories(); // Refresh list
        } catch (err: any) {
            alert(`Error updating category: ${err.message}`);
        }
    };

    if (isLoading) return <p className="text-center p-4">Loading categories...</p>;
    if (error) return <p className="text-center p-4 text-red-500">Error: {error}</p>;

    return (
        <div className="p-6 bg-white shadow-lg rounded-lg max-w-2xl mx-auto">
            <h2 className="text-2xl font-bold mb-6 text-gray-800">Category Settings</h2>

            {/* Add Category Form */}
            <form onSubmit={handleAddCategory} className="mb-8 p-4 border rounded">
                <h3 className="text-lg font-semibold mb-2">Add New Category</h3>
                <div className="flex gap-2">
                    <input
                        type="text"
                        value={newCategoryName}
                        onChange={(e) => setNewCategoryName(e.target.value)}
                        placeholder="Enter category name"
                        className="flex-grow p-2 border rounded focus:ring-2 focus:ring-blue-500 outline-none"
                    />
                    <button type="submit" className="px-4 py-2 bg-blue-500 text-white rounded hover:bg-blue-600">
                        Add
                    </button>
                </div>
            </form>

            {/* Edit Category Form (Modal-like or inline) */}
            {editingCategory && (
                <form onSubmit={handleSaveEdit} className="mb-8 p-4 border rounded bg-gray-50">
                    <h3 className="text-lg font-semibold mb-2">Edit Category: {editingCategory.name}</h3>
                     <div className="flex gap-2">
                        <input
                            type="text"
                            value={editName}
                            onChange={(e) => setEditName(e.target.value)}
                            className="flex-grow p-2 border rounded focus:ring-2 focus:ring-blue-500 outline-none"
                        />
                        <button type="submit" className="px-4 py-2 bg-green-500 text-white rounded hover:bg-green-600">
                            Save
                        </button>
                        <button type="button" onClick={() => setEditingCategory(null)} className="px-4 py-2 bg-gray-300 text-black rounded hover:bg-gray-400">
                            Cancel
                        </button>
                    </div>
                </form>
            )}

            {/* Category List */}
            <h3 className="text-lg font-semibold mb-4">Existing Categories</h3>
            {categories.length === 0 && !isLoading && <p>No categories found. Add some!</p>}
            <ul className="space-y-3">
                {categories.map((category) => (
                    <li key={category.id} className="flex justify-between items-center p-3 bg-gray-100 rounded shadow-sm">
                        <span className="text-gray-700">{category.name}</span>
                        <div className="space-x-2">
                            <button
                                onClick={() => handleEditCategory(category)}
                                className="px-3 py-1 text-sm bg-yellow-400 text-white rounded hover:bg-yellow-500"
                            >
                                Edit
                            </button>
                            <button
                                onClick={() => handleDeleteCategory(category.id)}
                                className="px-3 py-1 text-sm bg-red-500 text-white rounded hover:bg-red-600"
                            >
                                Delete
                            </button>
                        </div>
                    </li>
                ))}
            </ul>
        </div>
    );
};

export default CategorySettings;
