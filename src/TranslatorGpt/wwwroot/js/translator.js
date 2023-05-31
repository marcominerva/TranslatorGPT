function translator() {
    Alpine.data("translator", () => ({
        isBusy: false,
        errorMessage: null,

        provider: Alpine.$persist('OpenAI'),
        resourceName: null,
        apiKey: null,
        model: Alpine.$persist(null),

        sourceText: '',        
        language: Alpine.$persist(''),
        context: null,
        translations: [],

        reset: function () {
            this.translations = [];
            this.errorMessage = null;
            this.isBusy = false;
        },

        translate: async function () {
            while (this.isBusy)
            {
                await sleep(100);
            }

            if (this.sourceText == '' || this.language == '' || this.apiKey == null || this.model == null) {
                return;
            }

            this.reset();
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