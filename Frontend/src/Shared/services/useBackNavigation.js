import { useRouter } from 'vue-router';

export function useBackNavigation(fallbackRouteName = 'Main') {
    const router = useRouter();

    function goBack() {
        if (window.history.length > 1) {
            router.back();
            return;
        }

        router.push({ name: fallbackRouteName });
    }

    return {
        goBack,
    };
}

export default useBackNavigation;
