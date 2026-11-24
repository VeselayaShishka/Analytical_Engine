import { MantineProvider } from '@mantine/core';
import { Notifications } from '@mantine/notifications';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { RouterProvider } from 'react-router-dom';
import { router } from './app/router';
import { AuthProvider } from './features/auth/AuthContext';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      refetchOnWindowFocus: false,
      staleTime: 30_000,
    },
  },
});

export function App() {
  return (
      <QueryClientProvider client={queryClient}>
        <MantineProvider defaultColorScheme="light">
          <Notifications position="top-right" />
          <AuthProvider>
            <RouterProvider router={router} />
          </AuthProvider>
        </MantineProvider>
      </QueryClientProvider>
  );
}

export default App;