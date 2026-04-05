<template>
    <div class="info-section">
        <div v-for="card in cards" :key="card.title" class="info-card">
            <h1 class="default-text">{{ card.title }}</h1>
            <p class="description">{{ card.text }}</p>
        </div>
    </div>
</template>

<script setup>
const cards = [
    {
        title: "University Access",
        text: "Users can join a variety of universities available on the platform, each with unique content."
    },
    {
        title: "Collaboration",
        text: "Connect with peers within university communities to foster learning."
    },
    {
        title: "Forums",
        text: "Each university includes a forum system for sharing ideas and questions."
    },
    {
        title: "Responsive Design",
        text: "Designed to be user-friendly on both mobile and desktop devices."
    }
];
</script>

<style scoped>
.info-section {
    display: grid;
    grid-template-columns: 1fr;
    gap: 1rem;
    width: 100%;
}

.info-card {
    background-color: var(--ttl-bg-surface);
    border: 1px solid var(--ttl-accent-bright);
    padding: 1.5rem;
    border-radius: 12px;
    transition: transform 0.2s ease, box-shadow 0.2s ease;
}

.info-card:hover {
    box-shadow: 0 4px 15px rgba(179, 68, 198, 0.2);
}

.default-text {
    margin-bottom: 0.5rem;
    font-size: 1.4rem;
}

.description {
    color: var(--ttl-text-primary);
    font-family: var(--ttl-font-body);
    line-height: 1.5;
    margin: 0;
}

@media (min-width: 768px) {
    .info-section {
        grid-template-columns: repeat(2, 1fr);
        gap: 1.5rem;
    }

    .info-card:hover {
        transform: translateY(-5px);
    }
}
</style>