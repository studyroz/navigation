import {
  GraphQLInt,
  GraphQLList,
  GraphQLObjectType,
  GraphQLSchema,
  GraphQLString
} from "graphql";

var PEOPLE = [
  {
    id: 1,
    name: "Bell Halvorson",
    dateOfBirth: "01/01/1980",
    email: "bell@navigation.com",
    phone: "555 0001"
  },
  {
    id: 2,
    name: "Aditya Larson",
    dateOfBirth: "01/02/1980",
    email: "aditya@navigation.com",
    phone: "555 0002"
  },
  {
    id: 3,
    name: "Rashawn Schamberger",
    dateOfBirth: "01/03/1980",
    email: "rashawn@navigation.com",
    phone: "555 0003"
  },
  {
    id: 4,
    name: "Rupert Grant",
    dateOfBirth: "01/04/1980",
    email: "rupert@navigation.com",
    phone: "555 0004"
  },
  {
    id: 5,
    name: "Opal Carter",
    dateOfBirth: "01/05/1980",
    email: "opal@navigation.com",
    phone: "555 0005"
  },
  {
    id: 6,
    name: "Candida Christiansen",
    dateOfBirth: "01/06/1980",
    email: "candida@navigation.com",
    phone: "555 0006"
  },
  {
    id: 7,
    name: "Haven Stroman",
    dateOfBirth: "01/07/1980",
    email: "haven@navigation.com",
    phone: "555 0007"
  },
  {
    id: 8,
    name: "Celine Leannon",
    dateOfBirth: "01/08/1980",
    email: "celine@navigation.com",
    phone: "555 0008"
  },
  {
    id: 9,
    name: "Ryan Ruecker",
    dateOfBirth: "01/09/1980",
    email: "ryan@navigation.com",
    phone: "555 0009"
  },
  {
    id: 10,
    name: "Kaci Hoppe",
    dateOfBirth: "01/10/1980",
    email: "kaci@navigation.com",
    phone: "555 0010"
  },
  {
    id: 11,
    name: "Fernando Dietrich",
    dateOfBirth: "01/11/1980",
    email: "fernando@navigation.com",
    phone: "555 0011"
  },
  {
    id: 12,
    name: "Emelie Lueilwitz",
    dateOfBirth: "01/12/1980",
    email: "emelie@navigation.com",
    phone: "555 0012"
  }
];

var PersonType = new GraphQLObjectType({
  name: "Person",
  fields: () => ({
    id: { type: GraphQLString },
    name: { type: GraphQLString },
    dateOfBirth: { type: GraphQLString },
    email: { type: GraphQLString },
    phone: { type: GraphQLString }
  })
});

var PeopleType = new GraphQLObjectType({
  name: "People",
  fields: () => ({
    persons: { type: new GraphQLList(PersonType) }
  })
});

export default new GraphQLSchema({
  query: new GraphQLObjectType({
    name: "Query",
    fields: () => ({
      people: {
        type: PeopleType,
        args: {
          pageNumber: { type: GraphQLInt }
        },
        resolve: (root, { pageNumber }) => {
          var start = (pageNumber - 1) * 10;
          return { persons: PEOPLE.slice(start, start + 10) };
        }
      },
      person: {
        type: PersonType,
        args: {
          id: { type: GraphQLInt }
        },
        resolve: (root, { id }) => PEOPLE.filter(person => person.id === id)[0]
      }
    })
  })
});
