<template>
    <section class="info-section">
        <article v-for="card in cards" :key="card.title" class="info-card surface-card hover-lift">
            <div class="info-card__top">
                <span class="info-card__icon" :style="{ background: card.accent }">{{ card.short }}</span>
                <p class="info-card__tag">{{ card.tag }}</p>
            </div>
            <h3 class="info-card__title">{{ card.title }}</h3>
            <p class="description">{{ card.text }}</p>
        </article>
    </section>
</template>

<script setup>
const cards = [
    {
        short: 'UH',
        tag: 'Communities',
        title: 'University hubs',
        text: 'Keep courses, updates, and student discussions grouped by university so people always know where to go.',
        accent: 'linear-gradient(135deg, rgba(143, 44, 226, 0.95), rgba(255, 95, 162, 0.9))',
    },
    {
        short: 'LS',
        tag: 'Learning flow',
        title: 'Structured study paths',
        text: 'A calm layout keeps the next task visible, reduces friction, and helps students stay in motion.',
        accent: 'linear-gradient(135deg, rgba(255, 95, 162, 0.94), rgba(255, 177, 94, 0.9))',
    },
    {
        short: 'FM',
        tag: 'Conversation',
        title: 'Forum-first collaboration',
        text: 'Questions, replies, and peer help stay visible without the clutter of a heavy discussion product.',
        accent: 'linear-gradient(135deg, rgba(115, 120, 255, 0.95), rgba(143, 44, 226, 0.9))',
    },
    {
        short: 'AD',
        tag: 'Access everywhere',
        title: 'Responsive by default',
        text: 'The interface stays readable and touch-friendly on phones, laptops, and everything between.',
        accent: 'linear-gradient(135deg, rgba(143, 44, 226, 0.92), rgba(255, 95, 162, 0.92))',
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
    padding: 1.35rem;
}

.info-card__top {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 1rem;
    margin-bottom: 1rem;
}

.info-card__icon {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 2.6rem;
    height: 2.6rem;
    border-radius: 0.9rem;
    color: #ffffff;
    font-size: 0.92rem;
    font-weight: 800;
    letter-spacing: -0.03em;
    box-shadow: 0 10px 24px rgba(143, 44, 226, 0.18);
}

.info-card__tag {
    margin: 0;
    color: var(--ttl-text-secondary);
    font-size: 0.82rem;
    font-weight: 800;
    letter-spacing: 0.12em;
    text-transform: uppercase;
}

.info-card__title {
    margin: 0 0 0.6rem;
    color: var(--ttl-text-primary);
    font-size: 1.15rem;
    line-height: 1.15;
    letter-spacing: -0.02em;
}

.description {
    margin: 0;
    color: var(--ttl-text-secondary);
    font-size: 0.96rem;
    line-height: 1.65;
}

@media (min-width: 768px) {
    .info-section {
        grid-template-columns: repeat(2, 1fr);
        gap: 1.5rem;
    }
}
</style>