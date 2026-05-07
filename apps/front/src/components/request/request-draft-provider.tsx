"use client";

import {
  createContext,
  useContext,
  useEffect,
  useMemo,
  useReducer,
  useRef,
  type ReactNode,
} from "react";
import { requestDraftReducer } from "@/lib/request-draft/reducer";
import { loadRequestDraft, saveRequestDraft } from "@/lib/request-draft/storage";
import { emptyRequestDraft, type RequestDraftAction, type RequestDraftState } from "@/lib/request-draft/types";

type RequestDraftContextValue = {
  state: RequestDraftState;
  dispatch: React.Dispatch<RequestDraftAction>;
};

const RequestDraftContext = createContext<RequestDraftContextValue | null>(null);

export function RequestDraftProvider({ children }: { children: ReactNode }) {
  const [state, dispatch] = useReducer(requestDraftReducer, emptyRequestDraft);
  const didHydrate = useRef(false);

  useEffect(() => {
    dispatch({ type: "hydrate", state: loadRequestDraft() });
  }, []);

  useEffect(() => {
    if (!didHydrate.current) {
      didHydrate.current = true;
      return;
    }

    saveRequestDraft(state);
  }, [state]);

  const value = useMemo(() => ({ state, dispatch }), [state]);

  return <RequestDraftContext.Provider value={value}>{children}</RequestDraftContext.Provider>;
}

export function useRequestDraft() {
  const value = useContext(RequestDraftContext);
  if (!value) {
    throw new Error("useRequestDraft must be used inside RequestDraftProvider");
  }

  return value;
}
