# BeamEnumerator
ESAPI Script to enumerate beams and add setup fields

At our department we use kind of a special nomenclature for our PTVs and beams.
For example the largest PTV would be PTV1A and the first and second Boost PTV1BA and PTV1CBA.
The beam ids have three digits for site, plan number and plan revision, followed by two
digits for beam number. (11101 would be for site 1, first plan, first plan revision, beam 1.
12102 is first site, first boost, first revision, beam number 2).

The DRR parameters are guessed from the plan name and can be changed via a small gui. Also the
beam ids can be adapted. Beam order is always counterclockwise (except for ARCs) with field in field detection
through jaw size.
