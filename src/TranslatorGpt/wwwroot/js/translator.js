function translator() {
    Alpine.data("translator", () => ({
        isBusy: false,
        errorMessage: null,

        provider: Alpine.$persist('OpenAI'),
        resourceName: null,
        apiKey: null,
        model: Alpine.$persist(null),
        
        text: '',
        language: '',
        translations: [],
        isAsking: false,

        reset: function () {
            this.translations = [];
            this.errorMessage = null;
            this.isBusy = false;
        },

        translate: async function (url) {
            this.reset();
            this.isBusy = true;

            try {
                const response = await translateText(url);
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