import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { Document } from '~/types'
import { useAuth } from "~/composables/useAuth";

export const useWorkoutStore = defineStore("facet", () => {
  const auth = useAuth();
  const documents = ref<Document[]>([]);

  const loadDocuments = async () => {
    documents.value = await auth.fetchWithToken<Document[]>("Documents");
  };

  const addDocument = async (document: Document) => {
    const res = await auth.fetchWithToken("Documents", {
      method: "POST",
      body: document,
    });
  };

  return { documents, loadDocuments, addDocument };
});
