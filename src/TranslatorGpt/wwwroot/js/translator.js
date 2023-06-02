function translator() {
    Alpine.data("translator", () => ({
        isBusy: false,
        errorMessage: null,

        provider: Alpine.$persist('OpenAI'),
        resourceName: null,
        apiKey: '',
        model: Alpine.$persist(''),

        sourceText: '',        
        language: Alpine.$persist(''),
        context: null,
        translations: [],

        translate: async function () {
            this.errorMessage = null;
            this.translations = [];

            if (this.sourceText.trim().length == 0 || this.language == '') {
                return;
            }

            if (this.apiKey.trim().length == 0 || this.model.trim().length == 0) {
                this.errorMessage = 'Use the <a href="#" data-bs-toggle="modal" data-bs-target="#settings">Settings</a> dialog to set the API Key and the other requested configurations for OpenAI';
                return;
            }

            while (this.isBusy)
            {
                await sleep(100);
            }

            this.isBusy = true;

            try {
                const response = await translateText(this.sourceText, this.language, this.context, this.provider, this.apiKey, this.resourceName, this.model);
                const content = await response.json();

                this.isBusy = false;

                this.errorMessage = GetErrorMessage(response.status, content);

                if (this.errorMessage == null) {
                    // The request has succeeded.
                    this.translations = content;
                }
            } catch (error) {
                this.isBusy = false;
                this.errorMessage = error;
            }
        }
    }));
}