// src/components/ReportGenerator.tsx
import React, { useState } from 'react';

const API_REPORTS_URL = '/api/reports'; // Adjust if your API port is different

const ReportGenerator: React.FC = () => {
    const [samplesPerCategory, setSamplesPerCategory] = useState<string>("5");
    const [title, setTitle] = useState<string>("Image Classification Report");
    const [isLoading, setIsLoading] = useState<boolean>(false);
    const [error, setError] = useState<string | null>(null);
    const [successMessage, setSuccessMessage] = useState<string | null>(null);

    const handleGenerateReport = async () => {
        setIsLoading(true);
        setError(null);
        setSuccessMessage(null);

        const samples = parseInt(samplesPerCategory, 10);
        if (isNaN(samples) || samples < 0 || samples > 25) {
            setError("Samples per category must be a number between 0 and 25.");
            setIsLoading(false);
            return;
        }

        try {
            const response = await fetch(`${API_REPORTS_URL}/generate-pdf`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'application/pdf', // Important to tell the server we expect a PDF
                },
                body: JSON.stringify({
                    title: title.trim() === "" ? "Image Classification Report" : title,
                    samplesPerCategory: samples,
                }),
            });

            if (!response.ok) {
                // Try to parse error message if server sends JSON
                let errorMessage = `Error ${response.status}: ${response.statusText}`;
                try {
                    const errorData = await response.json();
                    errorMessage = errorData.message || errorData.title || errorMessage;
                } catch (e) {
                    // If response is not JSON, use the status text
                }
                throw new Error(errorMessage);
            }

            const contentType = response.headers.get("content-type");
            if (contentType && contentType.indexOf("application/pdf") !== -1) {
                const blob = await response.blob();
                const downloadUrl = window.URL.createObjectURL(blob);
                const link = document.createElement('a');
                link.href = downloadUrl;
                const reportFileName = `Report_${new Date().toISOString().split('T')[0]}.pdf`;
                link.setAttribute('download', reportFileName);
                document.body.appendChild(link);
                link.click();
                link.remove();
                window.URL.revokeObjectURL(downloadUrl); // Clean up
                setSuccessMessage(`Report "${reportFileName}" downloaded successfully.`);
            } else {
                // Unexpected content type
                let errorText = await response.text();
                throw new Error('Unexpected response from server. Expected PDF. Received: ' + (contentType || 'unknown') + '. Response: ' + errorText.substring(0,100));

            }

        } catch (err: any) {
            setError(err.message || "An unknown error occurred while generating the report.");
            console.error("Report generation error:", err);
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div className="p-4 border rounded-lg shadow-md bg-white">
            <h2 className="text-xl font-semibold mb-4 text-gray-700">Generate Report</h2>
            <div className="space-y-4">
                <div>
                    <label htmlFor="reportTitle" className="block text-sm font-medium text-gray-600 mb-1">
                        Report Title:
                    </label>
                    <input
                        type="text"
                        id="reportTitle"
                        value={title}
                        onChange={(e) => setTitle(e.target.value)}
                        placeholder="Enter report title"
                        className="w-full p-2 border border-gray-300 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500"
                    />
                </div>
                <div>
                    <label htmlFor="samplesPerCategory" className="block text-sm font-medium text-gray-600 mb-1">
                        Samples per Category (0-25):
                    </label>
                    <input
                        type="number"
                        id="samplesPerCategory"
                        value={samplesPerCategory}
                        onChange={(e) => setSamplesPerCategory(e.target.value)}
                        min="0"
                        max="25"
                        className="w-full p-2 border border-gray-300 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500"
                    />
                </div>
                <button
                    onClick={handleGenerateReport}
                    disabled={isLoading}
                    className="w-full px-4 py-2 bg-green-600 text-white font-semibold rounded-md shadow hover:bg-green-700 disabled:bg-gray-400 focus:outline-none focus:ring-2 focus:ring-green-500 focus:ring-offset-2"
                >
                    {isLoading ? 'Generating PDF...' : 'Download PDF Report'}
                </button>
                {successMessage && <p className="mt-2 text-sm text-green-600">{successMessage}</p>}
                {error && <p className="mt-2 text-sm text-red-600">{error}</p>}
            </div>
        </div>
    );
};

export default ReportGenerator;
