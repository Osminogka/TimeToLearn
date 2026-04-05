const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
const PASSWORD_DIGIT_REGEX = /\d/;
const PASSWORD_LOWER_REGEX = /[a-z]/;
const PASSWORD_UPPER_REGEX = /[A-Z]/;
const PASSWORD_SPECIAL_REGEX = /[^A-Za-z0-9]/;

function createFieldErrors() {
    return {
        name: [],
        email: [],
        password: [],
    };
}

function validateLoginForm({ email, password }) {
    const fieldErrors = createFieldErrors();

    if (!email) {
        fieldErrors.email.push('Email is required.');
    } else {
        if (!EMAIL_REGEX.test(email)) {
            fieldErrors.email.push('Enter a valid email address.');
        }
        if (email.length > 50) {
            fieldErrors.email.push('Email must be 50 characters or fewer.');
        }
    }

    if (!password) {
        fieldErrors.password.push('Password is required.');
    }

    return fieldErrors;
}

function validateRegisterForm({ name, email, password }) {
    const fieldErrors = createFieldErrors();

    if (!name) {
        fieldErrors.name.push('Username is required.');
    } else if (name.length > 50) {
        fieldErrors.name.push('Username must be 50 characters or fewer.');
    }

    if (!email) {
        fieldErrors.email.push('Email is required.');
    } else {
        if (!EMAIL_REGEX.test(email)) {
            fieldErrors.email.push('Enter a valid email address.');
        }
        if (email.length > 50) {
            fieldErrors.email.push('Email must be 50 characters or fewer.');
        }
    }

    if (!password) {
        fieldErrors.password.push('Password is required.');
    } else {
        if (password.length < 6) {
            fieldErrors.password.push('Password must be at least 6 characters.');
        }
        if (!PASSWORD_UPPER_REGEX.test(password)) {
            fieldErrors.password.push('Password must include at least one uppercase letter.');
        }
        if (!PASSWORD_LOWER_REGEX.test(password)) {
            fieldErrors.password.push('Password must include at least one lowercase letter.');
        }
        if (!PASSWORD_DIGIT_REGEX.test(password)) {
            fieldErrors.password.push('Password must include at least one number.');
        }
        if (!PASSWORD_SPECIAL_REGEX.test(password)) {
            fieldErrors.password.push('Password must include at least one special character.');
        }
    }

    return fieldErrors;
}

function mergeFieldErrors(target, source) {
    const merged = createFieldErrors();
    for (const key of Object.keys(merged)) {
        merged[key] = [...(target[key] || []), ...(source[key] || [])];
    }
    return merged;
}

function toFriendlyAuthErrors(message, mode, values) {
    const normalized = (message || '').trim().toLowerCase();
    const response = {
        general: [],
        fields: createFieldErrors(),
    };

    if (!normalized) {
        response.general.push('Unexpected error occurred. Please try again.');
        return response;
    }

    if (normalized.includes('invalid credentials')) {
        response.general.push('Please check your credentials and try again.');
        return response;
    }

    if (normalized.includes("user doesn't exist") || normalized.includes('user does not exist')) {
        response.fields.email.push('No account was found for this email.');
        return response;
    }

    if (normalized.includes('invalid password')) {
        response.fields.password.push('Password is incorrect.');
        return response;
    }

    if (normalized.includes('user already exist')) {
        response.fields.email.push('An account with this email already exists.');
        return response;
    }

    if (normalized.includes('name and email cannot be empty')) {
        if (!values.name) {
            response.fields.name.push('Username is required.');
        }
        if (!values.email) {
            response.fields.email.push('Email is required.');
        }
        return response;
    }

    if (normalized.includes('length must be under 50')) {
        if ((values.name || '').length > 50) {
            response.fields.name.push('Username must be 50 characters or fewer.');
        }
        if ((values.email || '').length > 50) {
            response.fields.email.push('Email must be 50 characters or fewer.');
        }
        return response;
    }

    if (mode === 'register' && normalized.includes("couldn't create user")) {
        response.fields.password.push('Password does not meet required policy.');
        response.general.push('Use at least 6 characters with uppercase, lowercase, number, and special character.');
        return response;
    }

    response.general.push(message);
    return response;
}

function collectSummaryMessages(fieldErrors, generalErrors) {
    const summary = [];
    for (const value of Object.values(fieldErrors)) {
        summary.push(...value);
    }
    summary.push(...generalErrors);
    return [...new Set(summary)];
}

export {
    collectSummaryMessages,
    mergeFieldErrors,
    toFriendlyAuthErrors,
    validateLoginForm,
    validateRegisterForm,
};
