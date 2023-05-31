async function translateText(text, language, context, provider, apiKey, resourceName, model) {
    const request = { text: text, language: language, context: context }
    const response = await fetch('/api/translate', {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
            "Authorization": `Bearer ${apiKey || ''}`,
            "x-provider": provider || '',
            "x-resource-name": resourceName || '',
            "x-model": model || ''
        },
        body: JSON.stringify(request)
    });

    return response;
}

function GetErrorMessage(statusCode, content)
{
    if (statusCode >= 200 && statusCode <= 299)
        return null;

    if (content.errors)
    {
        return `${content.title ?? content} (${content.errors[0].message})`;
    }

    return content.detail ?? content.title ?? content;
}

function sleep(time) {
    return new Promise((resolve) => {
        setTimeout(resolve, time);
    });
}

async function copyToClipboard(element, text)
{
    let tooltip = bootstrap.Tooltip.getInstance(element);
    tooltip.hide();

    navigator.clipboard.writeText(text);

    element.setAttribute('data-bs-title', 'Copied!');

    tooltip = new bootstrap.Tooltip(element);
    tooltip.show();

    await sleep(3000);
    tooltip.hide();

    // Resets the tooltip title
    element.setAttribute('data-bs-title', 'Copy to clipboard');
    new bootstrap.Tooltip(element);
}