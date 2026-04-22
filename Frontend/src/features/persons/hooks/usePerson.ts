import { useQuery } from "@tanstack/react-query";
import { getPerson } from "../../../api/persons";

export function usePerson(personId: string) {
  return useQuery({
    queryKey: ["person", personId],
    queryFn: () => getPerson(personId),
    enabled: !!personId,
  });
}