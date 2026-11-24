import { createBrowserRouter, Navigate } from 'react-router-dom';
import { AnalyticsBuilderPage } from '../features/analytics/AnalyticsBuilderPage';
import { LoginPage } from '../features/auth/LoginPage';
import { ProtectedRoute } from '../features/auth/ProtectedRoute';
import { RegisterPage } from '../features/auth/RegisterPage';
import { DatasetDetailsPage } from '../features/datasets/DatasetDetailsPage';
import { DatasetsListPage } from '../features/datasets/DatasetsListPage';
import { ImportPage } from '../features/import/ImportPage';
import { VersionRecordsPage } from '../features/records/VersionRecordsPage';
import { AppLayout } from '../shared/components/AppLayout';
import { DashboardPage } from '../features/dashboard/DashboardPage';
import { ReportDetailsPage } from '../features/reports/ReportDetailsPage';
import { ReportsListPage } from '../features/reports/ReportsListPage';

export const router = createBrowserRouter([
    {
        path: '/login',
        element: <LoginPage />,
    },
    {
        path: '/register',
        element: <RegisterPage />,
    },
    {
        element: <ProtectedRoute />,
        children: [
            {
                element: <AppLayout />,
                children: [
                    {
                        path: '/',
                        element: <DashboardPage />,
                    },
                    {
                        path: '/datasets',
                        element: <DatasetsListPage />,
                    },
                    {
                        path: '/datasets/:id',
                        element: <DatasetDetailsPage />,
                    },
                    {
                        path: '/datasets/:id/versions/:versionId',
                        element: <VersionRecordsPage />,
                    },
                    {
                        path: '/import',
                        element: <ImportPage />,
                    },
                    {
                        path: '/analytics',
                        element: <AnalyticsBuilderPage />,
                    },
                    {
                        path: '/reports',
                        element: <ReportsListPage />,
                    },
                    {
                        path: '/reports/:id',
                        element: <ReportDetailsPage />,
                    },
                ],
            },
        ],
    },
    {
        path: '*',
        element: <Navigate to="/" replace />,
    },
]);